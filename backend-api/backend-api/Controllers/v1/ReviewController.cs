using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models.DTOs.UpdateDTOs;
using backend_api.Repository;
using backend_api.Repository.IRepository;
using backend_api.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

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
        private readonly ILogger<TutorController> _logger;
        private readonly IMapper _mapper;
        private readonly FormatString _formatString;
        protected APIResponse _response;
        protected int pageSize = 0;

        public ReviewController(IReviewRepository reviewRepository,
            IUserRepository userRepository, ITutorRepository tutorRepository,
            ILogger<TutorController> logger,
            IMapper mapper, IConfiguration configuration,
            FormatString formatString)
        {
            _formatString = formatString;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _response = new APIResponse();
            _mapper = mapper;
            _logger = logger;
            _reviewRepository = reviewRepository;
            _userRepository = userRepository;
            _tutorRepository = tutorRepository;
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllReviews()
        {
            try
            {
                var reviews = await _reviewRepository.GetAllAsync();

                var reviewList = reviews.list.ToList();

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = reviewList;
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
                /*var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;*/

                if (reviewCreateDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }
                /*var reviewer = await _reviewRepository.GetReviewerByIdAsync(userId);
                var reviewee = await _reviewRepository.GetRevieweeByIdAsync(reviewCreateDTO.RevieweeId);*/

                /*if (reviewer == null || reviewee == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { "Reviewer or Reviewee not found" };
                    return NotFound(_response);
                }*/

                Review model = _mapper.Map<Review>(reviewCreateDTO);
                /*model.ReviewerId = userId;*/
                model.TutorId = reviewCreateDTO.RevieweeId;
                await _reviewRepository.CreateAsync(model);
                _response.StatusCode = HttpStatusCode.Created;
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

        [HttpPut("{id}")]
        public async Task<ActionResult<APIResponse>> UpdateReviewAsync(int id, [FromBody] ReviewUpdateDTO reviewUpdateDTO)
        {
            try
            {
                if (reviewUpdateDTO == null || id <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }

                var existingReview = await _reviewRepository.GetByIdAsync(id);
                if (existingReview == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { "Review not found" };
                    return NotFound(_response);
                }

                existingReview.RateScore = reviewUpdateDTO.RateScore;
                existingReview.Description = reviewUpdateDTO.Description;

                _reviewRepository.UpdateAsync(existingReview);

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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _reviewRepository.GetAsync(r => r.Id == id);
            if (review == null)
            {
                return NotFound();
            }
            await _reviewRepository.RemoveAsync(review);
            return NoContent();
        }
    }
}