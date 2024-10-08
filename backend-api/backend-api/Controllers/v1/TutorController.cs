using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Repository.IRepository;
using backend_api.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Net;

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
            ITutorRegistrationRequestRepository tutorRegistrationRequestRepository, ICurriculumRepository curriculumRepository)
        {
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
                    includeProperties: "User", pageSize: pageSize, pageNumber: pageNumber);
                list = result;
                totalCount = count;
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = totalCount };
                _response.Result = _mapper.Map<List<TutorDTO>>(list);
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
