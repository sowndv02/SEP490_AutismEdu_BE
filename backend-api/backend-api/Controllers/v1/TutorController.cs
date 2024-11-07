using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Repository.IRepository;
using backend_api.Services.IServices;
using backend_api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using static backend_api.SD;

namespace backend_api.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    //[Authorize]
    public class TutorController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ITutorRepository _tutorRepository;
        private readonly ITutorRequestRepository _tutorRequestRepository;
        private readonly ITutorProfileUpdateRequestRepository _tutorProfileUpdateRequestRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<TutorController> _logger;
        private readonly FormatString _formatString;
        protected APIResponse _response;
        protected int pageSize = 0;
        private readonly IResourceService _resourceService;

        public TutorController(IUserRepository userRepository, ITutorRepository tutorRepository,
            IMapper mapper, IConfiguration configuration,
            FormatString formatString, ITutorProfileUpdateRequestRepository tutorProfileUpdateRequestRepository, 
            ITutorRequestRepository tutorRequestRepository, IResourceService resourceService, ILogger<TutorController> logger)
        {
            _tutorProfileUpdateRequestRepository = tutorProfileUpdateRequestRepository;
            _formatString = formatString;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _response = new APIResponse();
            _mapper = mapper;
            _userRepository = userRepository;
            _tutorRepository = tutorRepository;
            _tutorRequestRepository = tutorRequestRepository;
            _resourceService = resourceService;
            _logger = logger;
        }

        [HttpGet("updateRequest")]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> GetAllUpdateRequestProfileAsync([FromQuery] string? status = SD.STATUS_ALL, string? orderBy = SD.CREATED_DATE, string? sort = SD.ORDER_DESC, int pageNumber = 1)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int totalCount = 0;
                List<TutorProfileUpdateRequest> list = new();
                Expression<Func<TutorProfileUpdateRequest, bool>> filter = u => u.TutorId == userId;
                Expression<Func<TutorProfileUpdateRequest, object>> orderByQuery = u => true;

                bool isDesc = !string.IsNullOrEmpty(sort) && sort == SD.ORDER_DESC;

                if (orderBy != null)
                {
                    switch (orderBy)
                    {
                        case SD.CREATED_DATE:
                            orderByQuery = x => x.CreatedDate;
                            break;
                        default:
                            orderByQuery = x => x.CreatedDate;
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(status) && status != SD.STATUS_ALL)
                {
                    switch (status.ToLower())
                    {
                        case "approve":
                            filter = filter.AndAlso(x => x.RequestStatus == Status.APPROVE);
                            break;
                        case "reject":
                            filter = filter.AndAlso(x => x.RequestStatus == Status.REJECT);
                            break;
                        case "pending":
                            filter = filter.AndAlso(x => x.RequestStatus == Status.PENDING);
                            break;
                    }
                }
                var (count, result) = await _tutorProfileUpdateRequestRepository.GetAllAsync(filter: filter, includeProperties: null, pageSize: 5, pageNumber: pageNumber, orderBy: orderByQuery, isDesc: isDesc);
                list = result;
                totalCount = count;
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = 5, Total = totalCount };
                _response.Result = _mapper.Map<List<TutorProfileUpdateRequestDTO>>(list);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving update request profiles for TutorId={TutorId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet("profile")]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> GetProfileTutor()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var (total, list) = await _tutorProfileUpdateRequestRepository.GetAllNotPagingAsync(x => x.TutorId == userId && x.RequestStatus == Status.PENDING, null, null);
                TutorProfileUpdateRequest model = null;
                model = list.OrderByDescending(x => x.CreatedDate).ToList().FirstOrDefault();
                Tutor result = null;
                if (model == null)
                {
                    var (totalResult, listResult) = await _tutorProfileUpdateRequestRepository.GetAllNotPagingAsync(x => x.TutorId == userId && x.RequestStatus == Status.APPROVE, null, null);
                    model = listResult.OrderByDescending(x => x.CreatedDate).ToList().FirstOrDefault();
                    if (model == null)
                    {
                        result = await _tutorRepository.GetAsync(x => x.TutorId == userId, false, "User", null);
                    }
                }
                if (model != null)
                {
                    _response.Result = _mapper.Map<TutorProfileUpdateRequestDTO>(model);
                }
                else if (result != null)
                {
                    _response.Result = _mapper.Map<TutorDTO>(result);
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the profile for TutorId={TutorId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse>> GetByIdAsync(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Bad request. Missing or invalid TutorId.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                var requests = new List<int>();
                if (userRoles != null && userRoles.Contains(SD.PARENT_ROLE))
                {
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var parent = await _userRepository.GetAsync(x => x.Id == userId, false, "TutorRequests");
                    var (total, listRequests) = await _tutorRequestRepository.GetAllNotPagingAsync(x => x.TutorId == id && x.ParentId == userId && x.RejectType == RejectType.IncompatibilityWithCurriculum, null, null);
                    requests = listRequests.Select(x => x.ChildId).ToList();
                }

                Tutor model = await _tutorRepository.GetAsync(x => x.TutorId == id, false, "User,Curriculums,AvailableTimeSlots,Certificates,WorkExperiences,Reviews");
                if (model == null)
                {
                    _logger.LogWarning("Tutor with TutorId={TutorId} not found", id);
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.TUTOR) };
                    return NotFound(_response);
                }
                model.TotalReview = model.Reviews.Count;
                model.ReviewScore = model.Reviews != null && model.Reviews.Any() ? model.Reviews.Average(x => x.RateScore) : 5;
                if (model == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.TUTOR) };
                    return NotFound(_response);
                }
                var result = _mapper.Map<TutorDTO>(model);
                result.RejectChildIds = requests;
                _response.Result = result;
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving tutor details for TutorId={TutorId}", id);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPut("{id}")]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<IActionResult> UpdateAsync(TutorProfileUpdateRequestCreateDTO updateDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                TutorProfileUpdateRequest model = _mapper.Map<TutorProfileUpdateRequest>(updateDTO);
                model.TutorId = userId;
                var result = await _tutorProfileUpdateRequestRepository.CreateAsync(model);
                _response.Result = _mapper.Map<TutorProfileUpdateRequestDTO>(result);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the TutorProfile for TutorId={TutorId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? search, string? searchAddress, int? reviewScore = 5, int? ageFrom = 0, int? ageTo = 15, int pageNumber = 1)
        {
            try
            {
                int totalCount = 0;
                List<Tutor> list = new();
                if (ageFrom == null || ageTo == null || ageFrom < 0 || ageTo == 0)
                {
                    ageFrom = 0;
                    ageTo = 15;
                }
                if (ageFrom > ageTo)
                {
                    var temp = ageFrom;
                    ageTo = ageFrom;
                    ageFrom = temp;
                }
                Expression<Func<Tutor, bool>> filterAge = u => u.StartAge >= ageFrom || u.EndAge <= ageTo;
                Expression<Func<Tutor, bool>> searchNameFilter = null;
                Expression<Func<Tutor, bool>> searchAddressFilter = null;
                if (!string.IsNullOrEmpty(search))
                {
                    searchNameFilter = u => u.User != null && !string.IsNullOrEmpty(u.User.FullName)
                    && u.User.FullName.ToLower().Contains(search.ToLower());
                }
                if (!string.IsNullOrEmpty(searchAddress))
                {
                    searchAddressFilter = u => u.User != null && !string.IsNullOrEmpty(u.User.Address)
                    && u.User.Address.ToLower().Contains(searchAddress.ToLower());
                }

                var (count, result) = await _tutorRepository.GetAllTutorAsync(filterName: searchNameFilter, filterAddress: searchAddressFilter, filterScore: reviewScore,
                    filterAge: filterAge, includeProperties: "User", pageSize: 9, pageNumber: pageNumber);
                list = result;
                totalCount = count;
                List<TutorDTO> tutorDTOList = _mapper.Map<List<TutorDTO>>(list);
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = 9, Total = totalCount };
                _response.Result = tutorDTOList;
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching tutors.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


    }
}
