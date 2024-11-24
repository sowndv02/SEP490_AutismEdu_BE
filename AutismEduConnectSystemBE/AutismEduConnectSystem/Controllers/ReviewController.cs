using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Models.DTOs;
using AutismEduConnectSystem.Models.DTOs.CreateDTOs;
using AutismEduConnectSystem.Models.DTOs.UpdateDTOs;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.SignalR;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Net;
using System.Security.Claims;

namespace AutismEduConnectSystem.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersionNeutral]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        protected int pageSize = 0;
        private readonly IResourceService _resourceService;
        private readonly INotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(IMapper mapper, IConfiguration configuration,
            IReviewRepository reviewRepository, IUserRepository userRepository
            , IResourceService resourceService,
            INotificationRepository notificationRepository, IHubContext<NotificationHub> hubContext,
            ILogger<ReviewController> logger)
        {
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _response = new APIResponse();
            _mapper = mapper;
            _reviewRepository = reviewRepository;
            _userRepository = userRepository;
            _resourceService = resourceService;
            _notificationRepository = notificationRepository;
            _hubContext = hubContext;
            _logger = logger;
        }

        [HttpGet("GetTutorReviewStats/{tutorId}")]
        public async Task<ActionResult<APIResponse>> GetTutorReviewInformation(string tutorId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                if (string.IsNullOrEmpty(tutorId))
                {
                    _logger.LogWarning("Bad request: tutorId is null or empty.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }

                var (totalReviews, reviews) = await _reviewRepository.GetAllAsync(
                    filter: x => x.TutorId == tutorId && !x.IsHide,
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
                _logger.LogError(ex, "Error occurred while retrieving tutor review information for tutorId: {TutorId}", tutorId);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllReviewsAsync(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var (totalCount, reviews) = await _reviewRepository.GetAllAsync(x => !x.IsHide,
                    includeProperties: "Parent,Tutor",
                    pageSize: pageSize,
                    pageNumber: pageNumber
                );

                if (reviews == null || !reviews.Any())
                {
                    _logger.LogWarning("No reviews found.");
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.REVIEW) };
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
                _logger.LogError(ex, "Error occurred while retrieving all reviews.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPost]
        [Authorize(Roles = SD.PARENT_ROLE)]
        public async Task<ActionResult<APIResponse>> CreateReviewAsync(ReviewCreateDTO reviewCreateDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                if (userRoles == null || (!userRoles.Contains(SD.PARENT_ROLE)))
                {
                   
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                
                if (reviewCreateDTO == null)
                {
                    _logger.LogWarning("Received null ReviewCreateDTO");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.REVIEW) };
                    return BadRequest(_response);
                }

                var existingReview = await _reviewRepository.GetAsync(x => x.TutorId == reviewCreateDTO.TutorId && x.ParentId == userId);
                if (existingReview != null)
                {
                    _logger.LogWarning("Duplicate review detected for tutor {TutorId} by user {UserId}", reviewCreateDTO.TutorId, userId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.REVIEW) };
                    return BadRequest(_response);
                }

                var reviewModel = _mapper.Map<Review>(reviewCreateDTO);
                reviewModel.ParentId = userId;
                reviewModel.TutorId = reviewCreateDTO.TutorId;
                reviewModel.CreatedDate = DateTime.Now;
                reviewModel.IsHide = false;

                var createdReview = await _reviewRepository.CreateAsync(reviewModel);

                var parent = await _userRepository.GetAsync(x => x.Id == userId);
                createdReview.Parent = parent;

                var reviewDTO = new ReviewDTO
                {
                    Id = createdReview.Id,
                    RateScore = createdReview.RateScore,
                    Description = createdReview.Description,
                    Parent = _mapper.Map<ApplicationUserDTO>(createdReview.Parent),
                    TutorId = createdReview.TutorId,
                    IsHide = createdReview.IsHide,
                    CreatedDate = createdReview.CreatedDate,
                    UpdatedDate = createdReview.UpdatedDate
                };

                // Notification
                var connectionId = NotificationHub.GetConnectionIdByUserId(reviewModel.TutorId);
                var notfication = new Notification()
                {
                    ReceiverId = reviewModel.TutorId,
                    Message = _resourceService.GetString(SD.NEW_REVIEW_TUTOR_NOTIFICATION),
                    UrlDetail = string.Concat(SD.URL_FE, SD.URL_FE_TUTOR_REVIEW_LIST),
                    IsRead = false,
                    CreatedDate = DateTime.Now
                };
                var notificationResult = await _notificationRepository.CreateAsync(notfication);
                if (!string.IsNullOrEmpty(connectionId))
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync($"Notifications-{reviewModel.TutorId}", _mapper.Map<NotificationDTO>(notificationResult));
                }
                _response.Result = reviewDTO;
                _response.StatusCode = HttpStatusCode.Created;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in CreateReviewAsync");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPut("{reviewId}")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> UpdateReviewAsync(int reviewId, ReviewUpdateDTO reviewUpdateDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                if (reviewUpdateDTO == null || reviewId == 0)
                {
                    _logger.LogWarning("Invalid input: ReviewUpdateDTO is null or reviewId is 0");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.REVIEW) };
                    return BadRequest(_response);
                }

                var existingReview = await _reviewRepository.GetAsync(x => x.Id == reviewId && x.ParentId == userId && !x.IsHide);

                if (existingReview == null)
                {
                    _logger.LogWarning("Review not found for reviewId {ReviewId} by user {UserId}", reviewId, userId);
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_ACTION_REVIEW) };
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
                _logger.LogError(ex, "Error occurred in EditReview for reviewId {ReviewId}", reviewId);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpDelete("{reviewId}")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> DeleteReview(int reviewId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                var review = await _reviewRepository.GetAsync(x => x.Id == reviewId && x.ParentId == userId);

                if (review == null)
                {
                    _logger.LogWarning("Review not found for reviewId {ReviewId} by user {UserId}", reviewId, userId);
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.REVIEW) };
                    return NotFound(_response);
                }
                review.IsHide = true;
                await _reviewRepository.RemoveAsync(review);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = SD.REVIEW_DELETE_SUCCESS;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in DeleteReview for reviewId {ReviewId}", reviewId);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
