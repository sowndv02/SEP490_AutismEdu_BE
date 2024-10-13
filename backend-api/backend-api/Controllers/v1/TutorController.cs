using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models.DTOs.UpdateDTOs;
using backend_api.Repository;
using backend_api.Repository.IRepository;
using backend_api.Utils;
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
            ITutorProfileUpdateRequestRepository tutorProfileUpdateRequestRepository)
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
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse>> GetByIdAsync(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }

                Tutor model = await _tutorRepository.GetAsync(x => x.UserId == id, false, "User,Curriculums,AvailableTimeSlots,Certificates");
                if (model == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.NOT_FOUND_MESSAGE };
                    return NotFound(_response);
                }
                var result = _mapper.Map<TutorDTO>(model);

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
                await _tutorProfileUpdateRequestRepository.CreateAsync(model);
                var user = await _userRepository.GetAsync(x => x.Id == userId, false, null);
                user.Address = updateDTO.Address;
                await _userRepository.UpdateAsync(user);
                var tutor = await _tutorRepository.GetAsync(x => x.UserId == userId, false, "User");
                tutor.Price = updateDTO.Price;
                await _tutorRepository.UpdateAsync(tutor);
                _response.Result = tutor;
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
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? seạrch, string? searchAddress, int? reviewScore = 5, int? ageFrom = 0, int? ageTo = 15, int pageNumber = 1)
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
                Expression<Func<Tutor, bool>> filter = u => u.StartAge >= ageFrom && u.EndAge <= ageTo;
                if (!string.IsNullOrEmpty(seạrch))
                {
                    Expression<Func<Tutor, bool>> searchNameFilter = u => u.User != null && !string.IsNullOrEmpty(u.User.FullName)
                    && u.User.FullName.ToLower().Contains(seạrch.ToLower());

                    var combinedFilter = Expression.Lambda<Func<Tutor, bool>>(
                        Expression.AndAlso(filter.Body, Expression.Invoke(searchNameFilter, filter.Parameters)),
                        filter.Parameters
                    );
                    filter = combinedFilter;
                }
                if (!string.IsNullOrEmpty(searchAddress))
                {
                    Expression<Func<Tutor, bool>> searchAddressFilter = u => u.User != null && !string.IsNullOrEmpty(u.User.Address)
                    && u.User.Address.ToLower().Contains(searchAddress.ToLower());

                    var combinedFilter = Expression.Lambda<Func<Tutor, bool>>(
                        Expression.AndAlso(filter.Body, Expression.Invoke(searchAddressFilter, filter.Parameters)),
                        filter.Parameters
                    );
                    filter = combinedFilter;
                }

                var (count, result) = await _tutorRepository.GetAllAsync(filter,
                    includeProperties: "User,Certificates,Curriculums,WorkExperiences", pageSize: 9, pageNumber: pageNumber);
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
