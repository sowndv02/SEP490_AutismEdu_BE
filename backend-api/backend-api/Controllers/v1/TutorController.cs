using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Repository;
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
    public class TutorController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ITutorRepository _tutorRepository;
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
            ICertificateRepository certificateRepository, ICertificateMediaRepository certificateMediaRepository)
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
        }


        [HttpPost]
        [Authorize]
        public async Task<ActionResult<APIResponse>> RegisterTutorAsync(TutorCreateDTO tutorCreateDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (tutorCreateDTO == null || string.IsNullOrEmpty(userId) || tutorCreateDTO.TutorInfo == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { $"Bad request!" };
                    return BadRequest(_response);
                }
                if (_tutorRepository.GetAsync(x => x.UserId.Equals(userId), true, null).GetAwaiter().GetResult() != null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.TUTOR_REGISTER_REQUEST_EXIST_OR_IS_TUTOR };
                    return BadRequest(_response);
                }
                tutorCreateDTO.TutorInfo.FormalName = _formatString.FormatStringFormalName(tutorCreateDTO.TutorInfo.FormalName);
                Tutor model = _mapper.Map<Tutor>(tutorCreateDTO.TutorInfo);
                model.UserId = userId;
                model.CreatedDate = DateTime.Now;
                await _tutorRepository.CreateAsync(model);

                // Add WorkExperience
                List<WorkExperience> modelWorkExperience = _mapper.Map<List<WorkExperience>>(tutorCreateDTO.WorkExperiences);
                foreach (var workExperience in modelWorkExperience) 
                {
                    workExperience.UserId = userId;
                    workExperience.CreatedDate = DateTime.Now;
                    await _workExperienceRepository.CreateAsync(workExperience);
                }

                // Add Certificate
                foreach (var cert in tutorCreateDTO.Certificates)
                {
                    var modelCertificate = _mapper.Map<Certificate>(cert);
                    modelCertificate.SubmiterId = userId;
                    modelCertificate.CreatedDate = DateTime.Now;
                    var certificate = await _certificateRepository.CreateAsync(modelCertificate);
                    foreach (var media in cert.Medias)
                    {
                        using var stream = media.OpenReadStream();
                        var url = await _blobStorageRepository.Upload(stream, userId + Path.GetExtension(media.FileName));
                        var objMedia = new CertificateMedia() { CertificateId = certificate.Id, UrlPath = url };
                        await _certificateMediaRepository.CreateAsync(objMedia);
                    }
                   
                }

                _response.StatusCode = HttpStatusCode.Created;
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
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? seạch, int pageNumber = 1)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int totalCount = 0;
                List<ApplicationUser> list = new();

                if (!string.IsNullOrEmpty(seạch))
                {
                    var (count, result) = await _userRepository.GetAllAsync(u => (u.Email.ToLower().Contains(seạch.ToLower())) || (!string.IsNullOrEmpty(u.TutorProfile.FormalName) && u.TutorProfile.FormalName.ToLower().Contains(seạch.ToLower())), "TutorProfile", pageSize: pageSize, pageNumber: pageNumber);
                    list = result;
                    totalCount = count;
                }
                else
                {
                    var (count, result) = await _userRepository.GetAllAsync(null, "TutorProfile",pageSize: pageSize, pageNumber: pageNumber);
                    list = result;
                    totalCount = count;
                }
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = totalCount };
                _response.Result = _mapper.Map<List<ApplicationUserDTO>>(list);
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
