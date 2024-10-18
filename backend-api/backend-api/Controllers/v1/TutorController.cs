using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Repository.IRepository;
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
        private readonly ITutorRegistrationRequestRepository _tutorRegistrationRequestRepository;
        private readonly ITutorProfileUpdateRequestRepository _tutorProfileUpdateRequestRepository;
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
        public TutorController(IUserRepository userRepository, ITutorRepository tutorRepository,
            ILogger<TutorController> logger, IBlobStorageRepository blobStorageRepository,
            IMapper mapper, IConfiguration configuration, IRoleRepository roleRepository,
            FormatString formatString, IWorkExperienceRepository workExperienceRepository,
            ICertificateRepository certificateRepository, ICertificateMediaRepository certificateMediaRepository,
            ITutorRegistrationRequestRepository tutorRegistrationRequestRepository, ICurriculumRepository curriculumRepository,
            ITutorProfileUpdateRequestRepository tutorProfileUpdateRequestRepository, ITutorRequestRepository tutorRequestRepository)
        {
            _tutorProfileUpdateRequestRepository = tutorProfileUpdateRequestRepository;
            _curriculumRepository = curriculumRepository;
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
            _tutorRequestRepository = tutorRequestRepository;
        }


        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetProfileTutor()
        {
            try
            {

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var (total, list) = await _tutorProfileUpdateRequestRepository.GetAllNotPagingAsync(x => x.TutorId == userId && x.RequestStatus == Status.PENDING, null, null);
                TutorProfileUpdateRequest model = null;
                model = list.OrderByDescending(x => x.CreatedDate).ToList().FirstOrDefault();
                Tutor result = null;
                if(model == null)
                {
                    var (totalResult, listResult) = await _tutorProfileUpdateRequestRepository.GetAllNotPagingAsync(x => x.TutorId == userId && x.RequestStatus == Status.APPROVE, null, null);
                    model = listResult.OrderByDescending(x => x.CreatedDate).ToList().FirstOrDefault();
                    if (model == null)
                    {
                        result = await _tutorRepository.GetAsync(x => x.UserId == userId, false, "User", null);
                    }
                }
                if(model != null)
                {
                    _response.Result = _mapper.Map<TutorProfileUpdateRequestDTO>(model);
                }else if(result != null)
                {
                    _response.Result = _mapper.Map<TutorDTO>(result);
                }
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


        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse>> GetByIdAsync(string id)
        {
            try
            {

                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                var requests = new List<int>();
                if (userRoles != null && userRoles.Contains(SD.PARENT_ROLE))
                {
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var parent = await _userRepository.GetAsync(x => x.Id == userId, false, "TutorRequests");
                    var (total, listRequests) = await _tutorRequestRepository.GetAllNotPagingAsync(x => x.TutorId == id && x.ParentId == userId && x.RejectType == RejectType.IncompatibilityWithCurriculum, null, null);
                    requests = listRequests.Select(x => x.ChildId).ToList();
                }


                if (string.IsNullOrEmpty(id))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }

                Tutor model = await _tutorRepository.GetAsync(x => x.UserId == id, false, "User,Curriculums,AvailableTimeSlots,Certificates,WorkExperiences,Reviews");
                model.TotalReview = model.Reviews.Count;
                model.ReviewScore = model.Reviews != null && model.Reviews.Any() ? model.Reviews.Average(x => x.RateScore) : 5;
                if (model == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.NOT_FOUND_MESSAGE };
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
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPut("{id}")]
        //[Authorize(Policy = "UpdateTutorPolicy")]
        public async Task<IActionResult> UpdateAsync(TutorProfileUpdateRequestCreateDTO updateDTO)
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
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
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
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


    }
}
