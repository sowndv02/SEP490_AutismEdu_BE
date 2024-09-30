using AutoMapper;
using backend_api.Migrations;
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
                // Update user profile
                var userProfile = await _userRepository.GetAsync(x => x.Id == userId);
                var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
                if (tutorCreateDTO.TutorBasicInfo.Image != null)
                {
                    if (!string.IsNullOrEmpty(tutorCreateDTO.TutorBasicInfo.ImageLocalPathUrl))
                    {
                        var oldFilePathDirectory = Path.Combine(Directory.GetCurrentDirectory(), tutorCreateDTO.TutorBasicInfo.ImageLocalPathUrl);
                        FileInfo file = new FileInfo(oldFilePathDirectory);
                        if (file.Exists)
                        {
                            file.Delete();
                        }
                    }
                    string fileName = userId + Path.GetExtension(tutorCreateDTO.TutorBasicInfo.Image.FileName);
                    string filePath = @"wwwroot\UserImage\" + fileName;

                    var directoryLocation = Path.Combine(Directory.GetCurrentDirectory(), filePath);

                    using (var fileStream = new FileStream(directoryLocation, FileMode.Create))
                    {
                        tutorCreateDTO.TutorBasicInfo.Image.CopyTo(fileStream);
                    }
                    userProfile.ImageLocalPathUrl = filePath;
                    userProfile.ImageLocalUrl = baseUrl + $"/{SD.URL_IMAGE_USER}/" + SD.IMAGE_DEFAULT_AVATAR_NAME;
                    using var stream = tutorCreateDTO.TutorBasicInfo.Image.OpenReadStream();
                    userProfile.ImageUrl = await _blobStorageRepository.Upload(stream, fileName);
                }
                if (!string.IsNullOrEmpty(tutorCreateDTO.TutorBasicInfo.Address))
                    userProfile.Address = tutorCreateDTO.TutorBasicInfo.Address;
                if (!string.IsNullOrEmpty(tutorCreateDTO.TutorBasicInfo.PhoneNumber))
                    userProfile.PhoneNumber = tutorCreateDTO.TutorBasicInfo.PhoneNumber;
                await _userRepository.UpdateAsync(userProfile);

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
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? seạch, int pageNumber = 1)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int totalCount = 0;
                List<Tutor> list = new();

                if (!string.IsNullOrEmpty(seạch))
                {
                    var (count, result) = await _tutorRepository.GetAllAsync(u => (!string.IsNullOrEmpty(u.FormalName) && u.FormalName.ToLower().Contains(seạch.ToLower())), "User", pageSize: pageSize, pageNumber: pageNumber, x => x.CreatedDate, true);
                    list = result;
                    totalCount = count;
                }
                else
                {
                    var (count, result) = await _tutorRepository.GetAllAsync(null, "User", pageSize: pageSize, pageNumber: pageNumber, x => x.CreatedDate, true);
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

        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse>> GetById(string id)
        {
            try
            {
                var result = await _tutorRepository.GetAsync(x => x.UserId == id, false, "User", null);
                _response.Result = _mapper.Map<TutorDTO>(result);
                _response.StatusCode = HttpStatusCode.OK;
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

        [HttpPut("changeStatus/{userId}")]
        [Authorize(Policy = "UpdateTutorPolicy")]
        public async Task<IActionResult> ApproveOrRejectTutor(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { $"{userId} is invalid!" };
                    return BadRequest(_response);
                }
                Tutor model = await _tutorRepository.GetAsync(x => x.UserId == userId, false, "User", null);
                model.IsApprove = !model.IsApprove;
                model.UpdatedDate = DateTime.Now;
                await _tutorRepository.UpdateAsync(model);

                // Update reject certificate
                if (!model.IsApprove)
                {
                    Certificate certificates = await _certificateRepository.GetAsync(x => x.SubmiterId == userId, false, "CertificateMedias", null);
                    model.IsApprove = false;
                    model.UpdatedDate = DateTime.Now;
                    await _certificateRepository.UpdateAsync(certificates);
                }
                _response.Result = _mapper.Map<TutorDTO>(model);
                _response.StatusCode = HttpStatusCode.OK;
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
