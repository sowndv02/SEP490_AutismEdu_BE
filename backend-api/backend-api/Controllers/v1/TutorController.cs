using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Repository.IRepository;
using backend_api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

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
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? seạrch, string? searchAddress, int? reviewScore = 5, int[]? ages = null, int pageNumber = 1)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int totalCount = 0;
                List<Tutor> list = new();
                if(ages == null || ages.Count() == 0)
                {
                    ages = new int[2] { 0, 15 };
                }
                if (ages[0] > ages[1])
                {
                    var temp = ages[0];
                    ages[1] = ages[0];
                    ages[0] = temp;
                }
                if (!string.IsNullOrEmpty(seạrch) || !string.IsNullOrEmpty(searchAddress) || reviewScore != null)
                {
                    var (countSearch, resultSearch) = await _tutorRepository.GetAllTutorAsync(u => (!string.IsNullOrEmpty(u.User.FullName) && u.User.FullName.ToLower().Contains(seạrch.ToLower())), 
                        filterAddress: u => (!string.IsNullOrEmpty(u.User.Address) && u.User.Address.ToLower().Contains(searchAddress.ToLower())), 
                        reviewScore: reviewScore, ageFrom: ages[0], ages[1],
                        includeProperties: "User,Reviews", pageSize: pageSize, pageNumber: pageNumber);
                    list = resultSearch;
                    totalCount = countSearch;
                }
                else
                {
                    var (count, result) = await _tutorRepository.GetAllTutorAsync(null, null, reviewScore: reviewScore, 
                        ageFrom: ages[0], ageTo: ages[1], "User,Reviews", pageSize: pageSize, pageNumber: pageNumber);
                    list = result;
                    totalCount = count;
                }
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
