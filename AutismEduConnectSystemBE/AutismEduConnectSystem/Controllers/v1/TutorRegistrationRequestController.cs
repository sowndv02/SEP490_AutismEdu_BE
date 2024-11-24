using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Models.DTOs;
using AutismEduConnectSystem.Models.DTOs.CreateDTOs;
using AutismEduConnectSystem.Models.DTOs.UpdateDTOs;
using AutismEduConnectSystem.RabbitMQSender;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.Utils;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.ConstrainedExecution;
using System.Security.Claims;
using static AutismEduConnectSystem.SD;

namespace AutismEduConnectSystem.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class TutorRegistrationRequestController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ITutorRepository _tutorRepository;
        private readonly IRabbitMQMessageSender _messageBus;
        private readonly ITutorRegistrationRequestRepository _tutorRegistrationRequestRepository;
        private readonly ICurriculumRepository _curriculumRepository;
        private readonly IWorkExperienceRepository _workExperienceRepository;
        private readonly ICertificateMediaRepository _certificateMediaRepository;
        private readonly ICertificateRepository _certificateRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IBlobStorageRepository _blobStorageRepository;
        private readonly ILogger<TutorRegistrationRequestController> _logger;
        private readonly IMapper _mapper;
        private string queueName = string.Empty;
        private readonly FormatString _formatString;
        protected APIResponse _response;
        protected int pageSize = 0;
        private readonly IResourceService _resourceService;

        public TutorRegistrationRequestController(IUserRepository userRepository, ITutorRepository tutorRepository,
            ILogger<TutorRegistrationRequestController> logger, IBlobStorageRepository blobStorageRepository,
            IMapper mapper, IConfiguration configuration, IRoleRepository roleRepository,
            FormatString formatString, IWorkExperienceRepository workExperienceRepository,
            ICertificateRepository certificateRepository, ICertificateMediaRepository certificateMediaRepository,
            ITutorRegistrationRequestRepository tutorRegistrationRequestRepository, ICurriculumRepository curriculumRepository,
            IRabbitMQMessageSender messageBus, IResourceService resourceService)
        {
            _messageBus = messageBus;
            _curriculumRepository = curriculumRepository;
            _formatString = formatString;
            _roleRepository = roleRepository;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            queueName = configuration["RabbitMQSettings:QueueName"];
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
            _resourceService = resourceService;
        }

        [HttpGet("{id}")]
        [Authorize(Roles = $"{SD.STAFF_ROLE},{SD.MANAGER_ROLE}")]
        public async Task<ActionResult<APIResponse>> GetByIdAsync(int id)
        {
            try
            {
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                if (userRoles == null || (!userRoles.Contains(SD.STAFF_ROLE) && !userRoles.Contains(SD.MANAGER_ROLE)))
                {
                   
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }

                if (id <= 0)
                {
                    _logger.LogWarning("Invalid Exercise ID: {Id}. Returning BadRequest.", id);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                var result = await _tutorRegistrationRequestRepository.GetAsync(x => x.Id == id, true, "ApprovedBy,Curriculums,WorkExperiences,Certificates", null);
                if (result == null)
                {
                    _logger.LogWarning("Exercise with ID {ExerciseId} not found for user: {UserId}", id, userId);
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.TUTOR_REGISTRATION_REQUEST) };
                    return NotFound(_response);
                }
                if(result.Certificates != null && result.Certificates.Any())
                {
                    foreach (var certificate in result.Certificates)
                    {
                        var (countMedias, medias) = await _certificateMediaRepository.GetAllNotPagingAsync(x => x.CertificateId == certificate.Id, includeProperties: null, excludeProperties: null);
                        certificate.CertificateMedias = medias;
                    }
                }
                _response.Result = _mapper.Map<TutorRegistrationRequestDTO>(result);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing GetAsync");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<APIResponse>> CreateAsync([FromForm] TutorRegistrationRequestCreateDTO tutorRegistrationRequestCreateDTO)
        {
            try
            {
                if (tutorRegistrationRequestCreateDTO == null || !ModelState.IsValid)
                {
                    _logger.LogWarning("Tutor registration request is null.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.TUTOR_REGISTRATION_REQUEST) };
                    return BadRequest(_response);
                }
                if (_tutorRegistrationRequestRepository.GetAsync(x => x.Email.Equals(tutorRegistrationRequestCreateDTO.Email) && (x.RequestStatus == Status.PENDING || x.RequestStatus == Status.APPROVE), true, null).GetAwaiter().GetResult() != null)
                {
                    _logger.LogWarning("A tutor registration request already exists or is in pending/approved status for email: {Email}", tutorRegistrationRequestCreateDTO.Email);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.TUTOR_REGISTER_REQUEST_EXIST_OR_IS_TUTOR) };
                    return BadRequest(_response);
                }
                if (_userRepository.GetAsync(x => x.Email.ToLower().Equals(tutorRegistrationRequestCreateDTO.Email.ToLower()), true, null).GetAwaiter().GetResult() != null)
                {
                    _logger.LogWarning("Email already exists: {Email}", tutorRegistrationRequestCreateDTO.Email);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.EMAIL_EXISTING_MESSAGE) };
                    return BadRequest(_response);
                }
                if (tutorRegistrationRequestCreateDTO.StartAge > tutorRegistrationRequestCreateDTO.EndAge)
                {
                    _logger.LogWarning("Invalid age range for tutor registration: {StartAge} - {EndAge}", tutorRegistrationRequestCreateDTO.StartAge, tutorRegistrationRequestCreateDTO.EndAge);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.TUTOR_REGISTRATION_REQUEST) };
                    return BadRequest(_response);
                }
                tutorRegistrationRequestCreateDTO.FullName = _formatString.FormatStringFormalName(tutorRegistrationRequestCreateDTO.FullName);
                TutorRegistrationRequest model = _mapper.Map<TutorRegistrationRequest>(tutorRegistrationRequestCreateDTO);

                if (tutorRegistrationRequestCreateDTO.Image != null)
                {
                    using var stream = tutorRegistrationRequestCreateDTO.Image.OpenReadStream();
                    model.ImageUrl = await _blobStorageRepository.Upload(stream, string.Concat(Guid.NewGuid().ToString(), Path.GetExtension(tutorRegistrationRequestCreateDTO.Image.FileName)));
                }

                model = await _tutorRegistrationRequestRepository.CreateAsync(model);

                // Handle certificate media uploads
                if (tutorRegistrationRequestCreateDTO.Certificates != null && model.Certificates != null)
                {
                    for (int i = 0; i < tutorRegistrationRequestCreateDTO.Certificates.Count; i++)
                    {
                        var certificateDTO = tutorRegistrationRequestCreateDTO.Certificates[i];
                        var certificate = model.Certificates[i];

                        if (certificateDTO.Medias != null && certificateDTO.Medias.Count > 0)
                        {
                            foreach (var media in certificateDTO.Medias)
                            {
                                using var mediaStream = media.OpenReadStream();
                                string mediaUrl = await _blobStorageRepository.Upload(mediaStream, string.Concat(Guid.NewGuid().ToString(), Path.GetExtension(media.FileName)));

                                CertificateMedia certificateMedia = new CertificateMedia
                                {
                                    CertificateId = certificate.Id,
                                    UrlPath = mediaUrl
                                };
                                await _certificateMediaRepository.CreateAsync(certificateMedia);
                            }
                        }
                    }
                }
                var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "TutorRegistrationRequestTemplate.cshtml");
                if (System.IO.File.Exists(templatePath))
                {
                    var subject = "Xác nhận Đăng ký Gia sư Dạy Trẻ Tự Kỷ - Đang Chờ Duyệt";
                    var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);
                    var htmlMessage = templateContent
                        .Replace("@Model.FullName", model.FullName)
                        .Replace("@Model.Email", model.Email)
                        .Replace("@Model.RegistrationDate", model.CreatedDate.ToString("dd/MM/yyyy"));
                    _messageBus.SendMessage(new EmailLogger()
                    {
                        Email = model.Email,
                        Subject = subject,
                        Message = htmlMessage
                    }, queueName);
                }
                _response.StatusCode = HttpStatusCode.Created;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the tutor registration request.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet]
        [Authorize(Roles = $"{SD.STAFF_ROLE},{SD.MANAGER_ROLE}")]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? search, string? status = SD.STATUS_ALL, DateTime? startDate = null, DateTime? endDate = null, string? orderBy = SD.CREATED_DATE, string? sort = SD.ORDER_DESC, int pageNumber = 1)
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
                if (userRoles == null || (!userRoles.Contains(SD.STAFF_ROLE) && !userRoles.Contains(SD.MANAGER_ROLE)))
                {
                   
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                Expression<Func<TutorRegistrationRequest, bool>> filter = u => true;
                Expression<Func<TutorRegistrationRequest, object>> orderByQuery = u => true;
                bool isDesc = sort != null && sort == SD.ORDER_DESC;
                if (!string.IsNullOrEmpty(search))
                {
                    filter = u => !string.IsNullOrEmpty(u.Email) && !string.IsNullOrEmpty(u.FullName) && (u.Email.ToLower().Contains(search.ToLower()) && u.FullName.ToLower().Contains(search.ToLower()));
                }

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
                else
                {
                    orderByQuery = x => x.CreatedDate;
                }
                if (startDate != null)
                {
                    filter = filter.AndAlso(u => u.CreatedDate.Date >= startDate.Value.Date);
                }
                if (endDate != null)
                {
                    filter = filter.AndAlso(u => u.CreatedDate.Date <= endDate.Value.Date);
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
                var (count, result) = await _tutorRegistrationRequestRepository.GetAllAsync(filter,
                                null, pageSize: pageSize, pageNumber: pageNumber, orderByQuery, isDesc);
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = count };
                _response.Result = _mapper.Map<List<TutorRegistrationRequestDTO>>(result);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing GetAllAsync");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPut("changeStatus/{id}")]
        [Authorize(Roles = $"{SD.STAFF_ROLE},{SD.MANAGER_ROLE}")]
        public async Task<ActionResult<APIResponse>> UpdateStatusRequest(ChangeStatusDTO tutorRegistrationRequestChange)
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
                if (userRoles == null || (!userRoles.Contains(SD.STAFF_ROLE) && !userRoles.Contains(SD.MANAGER_ROLE)))
                {
                   
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Status change to PENDING is not allowed for Tutor Registration Request {RequestId}.", tutorRegistrationRequestChange.Id);
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.TUTOR_REGISTRATION_REQUEST) };
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }
                if (tutorRegistrationRequestChange.StatusChange == (int)Status.PENDING)
                {
                    _logger.LogWarning("Status change to PENDING is not allowed for Tutor Registration Request {RequestId}.", tutorRegistrationRequestChange.Id);
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.TUTOR_UPDATE_STATUS_IS_PENDING) };
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }
                TutorRegistrationRequest model = await _tutorRegistrationRequestRepository.GetAsync(x => x.Id == tutorRegistrationRequestChange.Id, false, "Curriculums,WorkExperiences,Certificates", null);
                if(model == null)
                {
                    _logger.LogWarning("Status change to PENDING is not allowed for Tutor Registration Request {RequestId}.", tutorRegistrationRequestChange.Id);
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.TUTOR_REGISTRATION_REQUEST) };
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.NotFound;
                    return NotFound(_response);
                }
                if(model.RequestStatus != Status.PENDING)
                {
                    _logger.LogWarning("Status change to PENDING is not allowed for Tutor Registration Request {RequestId}.", tutorRegistrationRequestChange.Id);
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.TUTOR_REGISTRATION_REQUEST) };
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }
                if (tutorRegistrationRequestChange.StatusChange == (int)Status.APPROVE)
                {
                    string passsword = PasswordGenerator.GeneratePassword();
                    // Create user
                    var user = await _userRepository.CreateAsync(new ApplicationUser
                    {
                        Email = model.Email,
                        Address = model.Address,
                        FullName = model.FullName,
                        PhoneNumber = model.PhoneNumber,
                        EmailConfirmed = true,
                        IsLockedOut = false,
                        ImageUrl = model.ImageUrl,
                        CreatedDate = DateTime.Now,
                        UserName = model.Email,
                        UserType = SD.APPLICATION_USER,
                        LockoutEnabled = true,
                        RoleId = _roleRepository.GetByNameAsync(SD.TUTOR_ROLE).GetAwaiter().GetResult().Id
                    }, passsword);
                    if(user == null)
                    {
                        _logger.LogError("An error occurred while processing Tutor Registration Request {RequestId}.", tutorRegistrationRequestChange.Id);
                        _response.IsSuccess = false;
                        _response.StatusCode = HttpStatusCode.InternalServerError;
                        _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                        return StatusCode((int)HttpStatusCode.InternalServerError, _response);
                    }
                    // Create tutor profile
                    var tutor = await _tutorRepository.CreateAsync(new Tutor()
                    {
                        TutorId = user.Id,
                        PriceFrom = model.PriceFrom,
                        PriceEnd = model.PriceEnd,
                        AboutMe = model.AboutMe,
                        DateOfBirth = model.DateOfBirth,
                        StartAge = model.StartAge,
                        EndAge = model.EndAge,
                        SessionHours = model.SessionHours,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    });
                    if (tutor == null)
                    {
                        _logger.LogError("An error occurred while processing Tutor Registration Request {RequestId}.", tutorRegistrationRequestChange.Id);
                        _response.IsSuccess = false;
                        _response.StatusCode = HttpStatusCode.InternalServerError;
                        _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                        return StatusCode((int)HttpStatusCode.InternalServerError, _response);
                    }
                    // Update status curriculum
                    if (model.Curriculums != null)
                    {
                        var curiculums = model.Curriculums.Where(x => x.RequestStatus == Status.PENDING).ToList();
                        await UpdateStatusCurriculums(curiculums, userId, string.Empty, Status.APPROVE, tutor.TutorId);
                    }
                    // Update status certificate except certificate have status is reject
                    if (model.Certificates != null)
                    {
                        var certificates = model.Certificates.Where(x => x.RequestStatus == Status.PENDING).ToList();
                        await UpdateStatusCertificates(certificates, userId, string.Empty, Status.APPROVE,tutor.TutorId);
                    }
                    // Update status work experience
                    if (model.WorkExperiences != null)
                    {
                        var workExperiences = model.WorkExperiences.Where(x => x.RequestStatus == Status.PENDING).ToList();
                        await UpdateStatusWorkExperiences(workExperiences, userId, string.Empty, Status.APPROVE, tutor.TutorId);
                    }

                    model.RequestStatus = Status.APPROVE;
                    model.UpdatedDate = DateTime.Now;
                    model.ApprovedId = userId;
                    await _tutorRegistrationRequestRepository.UpdateAsync(model);
                    
                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "AcceptedTutorRegistrationRequest.cshtml");
                    if (!System.IO.File.Exists(templatePath))
                    {
                        _response.IsSuccess = false;
                        _response.StatusCode = HttpStatusCode.InternalServerError;
                        _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                        return StatusCode((int)HttpStatusCode.InternalServerError, _response);
                    }
                    var subject = "Thông báo Chấp nhận Đơn Đăng ký Gia sư Dạy Trẻ Tự Kỷ";
                    var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);
                    var htmlMessage = templateContent
                        .Replace("@Model.FullName", model.FullName)
                        .Replace("@Model.Username", model.Email)
                        .Replace("@Model.Password", passsword)
                        .Replace("@Model.LoginUrl", SD.URL_FE_TUTOR_LOGIN);

                    _messageBus.SendMessage(new EmailLogger()
                    {
                        UserId = user.Id,
                        Email = model.Email,
                        Subject = subject,
                        Message = htmlMessage
                    }, queueName);
                    _response.Result = _mapper.Map<TutorRegistrationRequestDTO>(model);
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    return Ok(_response);
                }
                else if (tutorRegistrationRequestChange.StatusChange == (int)Status.REJECT)
                {
                    // Handle for reject
                    model.RejectionReason = tutorRegistrationRequestChange.RejectionReason;
                    model.UpdatedDate = DateTime.Now;
                    model.RequestStatus = Status.REJECT;
                    model.ApprovedId = userId;
                    await _tutorRegistrationRequestRepository.UpdateAsync(model);

                    // Reject certificate
                    if (model.Certificates != null)
                    {
                        await UpdateStatusCertificates(model.Certificates, userId, tutorRegistrationRequestChange.RejectionReason, Status.REJECT, string.Empty);
                    }
                    // Reject curriculum
                    if (model.Curriculums != null)
                    {
                        await UpdateStatusCurriculums(model.Curriculums, userId, tutorRegistrationRequestChange.RejectionReason, Status.REJECT, string.Empty);
                    }
                    if (model.WorkExperiences != null)
                    {
                        await UpdateStatusWorkExperiences(model.WorkExperiences, userId, tutorRegistrationRequestChange.RejectionReason, Status.REJECT, string.Empty);
                    }
                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "RejectTutorRegistrationRequest.cshtml");
                    if (System.IO.File.Exists(templatePath))
                    {
                        var subject = "Thông báo Từ chối Đơn Đăng ký Gia sư và Hướng dẫn Tạo Đơn Mới";
                        var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);
                        var htmlMessage = templateContent
                            .Replace("@Model.FullName", model.FullName)
                            .Replace("@Model.RejectionReason", model.RejectionReason ?? "Không có lý do cụ thể.");

                        _messageBus.SendMessage(new EmailLogger()
                        {
                            Email = model.Email,
                            Subject = subject,
                            Message = htmlMessage
                        }, queueName);
                    }
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.Result = _mapper.Map<TutorRegistrationRequestDTO>(model);
                    _response.IsSuccess = true;
                    return Ok(_response);

                }
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing Tutor Registration Request {RequestId}.", tutorRegistrationRequestChange.Id);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        private async Task UpdateStatusWorkExperiences(List<WorkExperience> list, string userId, string rejectReason, Status status, string tutorId)
        {
            foreach (var workExperience in list)
            {
                if (status == Status.REJECT)
                {
                    workExperience.ApprovedId = userId;
                    workExperience.UpdatedDate = DateTime.Now;
                    workExperience.IsActive = false;
                    workExperience.RejectionReason = rejectReason;
                    workExperience.RequestStatus = Status.REJECT;
                    await _workExperienceRepository.UpdateAsync(workExperience);
                }
                else
                {
                    workExperience.RequestStatus = Status.APPROVE;
                    workExperience.SubmitterId = tutorId;
                    workExperience.ApprovedId = userId;
                    workExperience.IsActive = true;
                    workExperience.UpdatedDate = DateTime.Now;
                    await _workExperienceRepository.UpdateAsync(workExperience);
                }

            }
        }

        private async Task UpdateStatusCurriculums(List<Curriculum> curriculums, string userId, string rejectReason, Status status, string tutorId)
        {
            foreach (var item in curriculums)
            {
                if(status == Status.REJECT)
                {
                    item.ApprovedId = userId;
                    item.IsActive = false;
                    item.UpdatedDate = DateTime.Now;
                    item.RejectionReason = rejectReason;
                    item.RequestStatus = status;
                    await _curriculumRepository.UpdateAsync(item);
                }
                else
                {
                    item.RequestStatus = Status.APPROVE;
                    item.ApprovedId = userId;
                    item.IsActive = true;
                    item.UpdatedDate = DateTime.Now;
                    item.SubmitterId = tutorId;
                    await _curriculumRepository.UpdateAsync(item);
                }
                
            }
        }

        private async Task UpdateStatusCertificates(List<Certificate> list, string userId, string rejectReason, Status status, string tutorId)
        {
            foreach (var item in list)
            {
                if (status == Status.REJECT)
                {
                    item.ApprovedId = userId;
                    item.IsDeleted = true;
                    item.UpdatedDate = DateTime.Now;
                    item.RejectionReason = rejectReason;
                    item.RequestStatus = Status.REJECT;
                    await _certificateRepository.UpdateAsync(item);
                }
                else
                {
                    item.RequestStatus = Status.APPROVE;
                    item.SubmitterId = tutorId;
                    item.ApprovedId = userId;
                    item.UpdatedDate = DateTime.Now;
                    await _certificateRepository.UpdateAsync(item);
                }

            }
        }

    }
}
