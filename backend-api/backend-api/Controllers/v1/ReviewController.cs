using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs.UpdateDTOs;
using backend_api.Models.DTOs;
using backend_api.Repository.IRepository;
using backend_api.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using backend_api.Models.DTOs.CreateDTOs;

namespace backend_api.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IUserRepository _userRepository;
        private readonly ITutorRepository _tutorRepository;
        private readonly ITutorRegistrationRequestRepository _tutorRegistrationRequestRepository;
        private readonly ICurriculumRepository _curriculumRepository;
        private readonly IWorkExperienceRepository _workExperienceRepository;
        private readonly ICertificateMediaRepository _certificateMediaRepository;
        private readonly ICertificateRepository _certificateRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IBlobStorageRepository _blobStorageRepository;
        private readonly ILogger<TutorController> _logger;
        private readonly IMapper _mapper;
        private readonly FormatString _formatString;
        protected APIResponse _response;
        protected int pageSize = 0;

        public ReviewController(IUserRepository userRepository, ITutorRepository tutorRepository,
            ILogger<TutorController> logger, IBlobStorageRepository blobStorageRepository,
            IMapper mapper, IConfiguration configuration, IRoleRepository roleRepository,
            FormatString formatString, IWorkExperienceRepository workExperienceRepository,
            ICertificateRepository certificateRepository, ICertificateMediaRepository certificateMediaRepository,
            ITutorRegistrationRequestRepository tutorRegistrationRequestRepository, IReviewRepository reviewRepository)
        {
            _formatString = formatString;
            _roleRepository = roleRepository;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _response = new APIResponse();
            _mapper = mapper;
            _blobStorageRepository = blobStorageRepository;
            _logger = logger;
            _userRepository = userRepository;
            _tutorRepository = tutorRepository;
            _workExperienceRepository = workExperienceRepository;
            _certificateRepository = certificateRepository;
            _certificateMediaRepository = certificateMediaRepository;
            _tutorRegistrationRequestRepository = tutorRegistrationRequestRepository;
            _reviewRepository = reviewRepository;
        }

        [HttpGet("GetTutorReviewStats/{tutorId}")]
        public async Task<ActionResult<APIResponse>> GetTutorReviewInformation(string tutorId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                if (string.IsNullOrEmpty(tutorId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }

                var (totalReviews, reviews) = await _reviewRepository.GetAllAsync(
                    filter: x => x.TutorId == tutorId,
                    includeProperties: "Parent",
                    pageSize: pageSize,
                    pageNumber: pageNumber,
                    orderBy: x => x.CreatedDate,
                    isDesc: true
                    );

                decimal averageScore = totalReviews > 0 ? reviews.Average(x => x.RateScore) : 0;

                var scoreRanges = new Dictionary<string, int>
                {
                    { "5", 0 },
                    { "4", 0 },
                    { "3", 0 },
                    { "2", 0 },
                    { "1", 0 }
                };

                foreach (var review in reviews)
                {
                    var score = review.RateScore switch
                    {
                        >= 0 and < 2 => "1",
                        >= 2 and < 3 => "2",
                        >= 3 and < 4 => "3",
                        >= 4 and < 5 => "4",
                        5 => "5",
                        _ => "Unknown Score"
                    };

                    if (scoreRanges.ContainsKey(score))
                        scoreRanges[score]++;
                }

                var scoreGroups = scoreRanges.Select(sr => new
                {
                    ScoreRange = sr.Key,
                    ReviewCount = sr.Value
                })
                .OrderByDescending(sr => sr.ScoreRange)
                .ToList();

                var reviewsDTO = reviews.Select(r => new
                {
                    r.Id,
                    r.RateScore,
                    r.Description,
                    r.TutorId,
                    Parent = new
                    {
                        r.Parent.Id,
                        r.Parent.FullName,
                        r.Parent.ImageUrl
                    },
                    r.CreatedDate
                }).ToList();

                _response.Result = new
                {
                    TotalReviews = totalReviews,
                    AverageScore = averageScore,
                    ScoreGroups = scoreGroups,
                    Reviews = reviewsDTO
                };
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllReviewsAsync(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var (totalCount, reviews) = await _reviewRepository.GetAllAsync(
                    includeProperties: "Parent,Tutor",
                    pageSize: pageSize,
                    pageNumber: pageNumber
                );

                if (reviews == null || !reviews.Any())
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.NO_REVIEWS_FOUND };
                    return NotFound(_response);
                }

                var reviewDTOs = _mapper.Map<List<ReviewDTO>>(reviews);

                _response.Result = new
                {
                    TotalCount = totalCount,
                    PageSize = pageSize,
                    CurrentPage = pageNumber,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    Reviews = reviewDTOs
                };
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPost]
        public async Task<ActionResult<APIResponse>> CreateReviewAsync(ReviewCreateDTO reviewCreateDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (reviewCreateDTO == null || string.IsNullOrEmpty(userId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }

                var existingReview = await _reviewRepository.GetAsync(x => x.TutorId == reviewCreateDTO.TutorId && x.ParentId == userId);
                if (existingReview != null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.REVIEW_ALREADY_EXISTS };
                    return BadRequest(_response);
                }

                var reviewModel = _mapper.Map<Review>(reviewCreateDTO);
                reviewModel.ParentId = userId;  
                reviewModel.TutorId = reviewCreateDTO.TutorId;  
                reviewModel.CreatedDate = DateTime.Now;

                var createdReview = await _reviewRepository.CreateAsync(reviewModel);

                var parent = await _userRepository.GetAsync(x => x.Id == userId);
                createdReview.Parent = parent;

                var reviewDTO = new ReviewDTO
                {
                    Id = createdReview.Id,
                    RateScore = createdReview.RateScore,
                    Description = createdReview.Description,
                    Parent = createdReview.Parent,
                    TutorId = createdReview.TutorId,
                    CreatedDate = createdReview.CreatedDate,
                    UpdatedDate = createdReview.UpdatedDate
                };

                _response.Result = reviewDTO;
                _response.StatusCode = HttpStatusCode.Created;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPut("{reviewId}")]
        public async Task<ActionResult<APIResponse>> EditReview(int reviewId, ReviewUpdateDTO reviewUpdateDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (reviewUpdateDTO == null || reviewId == 0 || string.IsNullOrEmpty(userId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }

                var existingReview = await _reviewRepository.GetAsync(x => x.Id == reviewId && x.ParentId == userId);

                if (existingReview == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_ACTION_REVIEW };
                    return NotFound(_response);
                }

                existingReview.RateScore = reviewUpdateDTO.RateScore;
                existingReview.Description = reviewUpdateDTO.Description;
                existingReview.UpdatedDate = DateTime.Now;

                await _reviewRepository.UpdateAsync(existingReview);

                _response.Result = _mapper.Map<ReviewDTO>(existingReview);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpDelete("{reviewId}")]
        public async Task<ActionResult<APIResponse>> DeleteReview(int reviewId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }

                var review = await _reviewRepository.GetAsync(x => x.Id == reviewId && x.ParentId == userId);

                if (review == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_ACTION_REVIEW };
                    return NotFound(_response);
                }

                await _reviewRepository.RemoveAsync(review);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = SD.REVIEW_DELETE_SUCCESS;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
