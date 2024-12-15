using AutismEduConnectSystem.DTOs;
using AutismEduConnectSystem.DTOs.CreateDTOs;
using AutismEduConnectSystem.DTOs.UpdateDTOs;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.SignalR;
using AutismEduConnectSystem.Utils;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using static AutismEduConnectSystem.SD;

namespace AutismEduConnectSystem.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class CertificateController : ControllerBase
    {

        private readonly IUserRepository _userRepository;
        private readonly ICertificateRepository _certificateRepository;
        private readonly ICertificateMediaRepository _certificateMediaRepository;
        private readonly ICurriculumRepository _curriculumRepository;
        private readonly IWorkExperienceRepository _workExperienceRepository;
        private readonly IBlobStorageRepository _blobStorageRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<CertificateController> _logger;
        private readonly IMapper _mapper;
        private readonly IEmailSender _messageBus;
        private string queueName = string.Empty;
        protected APIResponse _response;
        protected int pageSize = 0;
        private readonly IResourceService _resourceService;
        private readonly IHubContext<NotificationHub> _hubContext;


        public CertificateController(IUserRepository userRepository, ICertificateRepository certificateRepository,
            ILogger<CertificateController> logger, IBlobStorageRepository blobStorageRepository,
            IMapper mapper, IConfiguration configuration,
            ICertificateMediaRepository certificateMediaRepository, ICurriculumRepository curriculumRepository,
            IWorkExperienceRepository workExperienceRepository, IEmailSender messageBus, IResourceService resourceService,
            IHubContext<NotificationHub> hubContext, INotificationRepository notificationRepository)
        {
            _resourceService = resourceService;
            _workExperienceRepository = workExperienceRepository;
            _curriculumRepository = curriculumRepository;
            _certificateMediaRepository = certificateMediaRepository;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            queueName = configuration["RabbitMQSettings:QueueName"];
            _response = new APIResponse();
            _mapper = mapper;
            _blobStorageRepository = blobStorageRepository;
            _logger = logger;
            _hubContext = hubContext;
            _userRepository = userRepository;
            _certificateRepository = certificateRepository;
            _messageBus = messageBus;
            _notificationRepository = notificationRepository;
        }


        [HttpGet("{id}")]
        [Authorize(Roles = $"{SD.MANAGER_ROLE},{SD.TUTOR_ROLE},{SD.STAFF_ROLE}")]
        public async Task<ActionResult<APIResponse>> GetByIdAsync(int id)
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
                if (userRoles == null || (!userRoles.Contains(SD.STAFF_ROLE) && !userRoles.Contains(SD.MANAGER_ROLE) && !userRoles.Contains(SD.TUTOR_ROLE)))
                {
                   
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                if (id <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                Certificate result = null;
                if (userRoles != null && userRoles.Contains(SD.TUTOR_ROLE))
                {
                    result = await _certificateRepository.GetAsync(x => x.Id == id && !x.IsDeleted, false, "CertificateMedias", null);
                }
                else if (userRoles != null && (userRoles.Contains(SD.MANAGER_ROLE) || userRoles.Contains(SD.STAFF_ROLE)))
                {
                    result = await _certificateRepository.GetAsync(x => x.Id == id, false, "CertificateMedias", null);
                }

                if (result == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.CERTIFICATE) };
                    return NotFound(_response);
                }
                _response.StatusCode = HttpStatusCode.Created;
                _response.Result = _mapper.Map<CertificateDTO>(result);
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> CreateAsync([FromForm] CertificateCreateDTO createDTO)
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
            if (userRoles == null || (!userRoles.Contains(SD.TUTOR_ROLE)))
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.Forbidden;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                return StatusCode((int)HttpStatusCode.Forbidden, _response);
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.CERTIFICATE) };
                    return BadRequest(_response);
                }

                var newModel = _mapper.Map<Certificate>(createDTO);
                if (newModel.ExpirationDate == DateTime.MinValue || string.IsNullOrEmpty(newModel.ExpirationDate?.ToString())) newModel.ExpirationDate = null;
                newModel.SubmitterId = userId;

                var certificateExtist = await _certificateRepository.GetAllNotPagingAsync(x => x.CertificateName.Equals(newModel.CertificateName));
                if (certificateExtist.list.Any())
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.CERTIFICATE) };
                    return BadRequest(_response);
                }

                var certificate = await _certificateRepository.CreateAsync(newModel);
                if (createDTO.Medias != null && createDTO.Medias.Any()) 
                {
                    foreach (var media in createDTO.Medias)
                    {
                        using var stream = media.OpenReadStream();
                        var url = await _blobStorageRepository.Upload(stream, string.Concat(Guid.NewGuid().ToString(), Path.GetExtension(media.FileName)));
                        var objMedia = new CertificateMedia() { CertificateId = certificate.Id, UrlPath = url, CreatedDate = DateTime.Now };
                        await _certificateMediaRepository.CreateAsync(objMedia);
                    }
                }

                _response.StatusCode = HttpStatusCode.Created;
                _response.Result = _mapper.Map<CertificateDTO>(newModel);
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPut("changeStatus/{id}")]
        [Authorize(Roles = $"{SD.STAFF_ROLE},{SD.MANAGER_ROLE}")]
        public async Task<ActionResult<APIResponse>> UpdateStatusRequest(int id, ChangeStatusDTO changeStatusDTO)
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

            try
            {
                Certificate model = await _certificateRepository.GetAsync(x => x.Id == changeStatusDTO.Id, false, "CertificateMedias", null);
                var tutor = await _userRepository.GetAsync(x => x.Id == model.SubmitterId, false, null);
                if (model == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.CERTIFICATE) };
                    return NotFound(_response);
                }
                if(id != changeStatusDTO.Id)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                if ( model.RequestStatus != Status.PENDING)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.CANNOT_UPDATE_BECAUSE_NOT_PENDING, SD.CERTIFICATE) };
                    return BadRequest(_response);
                }
                if (changeStatusDTO.StatusChange == (int)Status.APPROVE)
                {
                    model.RequestStatus = Status.APPROVE;
                    model.UpdatedDate = DateTime.Now;
                    model.ApprovedId = userId;
                    await _certificateRepository.UpdateAsync(model);

                    // Send mail
                    var subject = "Yêu cập nhật chứng chỉ của bạn đã được chấp nhận!";
                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ChangeStatusTemplate.cshtml");
                    if (System.IO.File.Exists(templatePath) && tutor != null)
                    {
                        var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);

                        var rejectionReasonHtml = string.Empty;
                        var htmlMessage = templateContent
                        .Replace("@Model.FullName", tutor.FullName)
                        .Replace("@Model.IssueName", $"Yêu cầu cập nhật chứng chỉ {model.CertificateName} của bạn")
                        .Replace("@Model.IsApprovedString", "Chấp nhận")
                        .Replace("@Model.RejectionReason", rejectionReasonHtml)
                        .Replace("@Model.Mail", SD.MAIL)
                        .Replace("@Model.Phone", SD.PHONE_NUMBER)
                        .Replace("@Model.WebsiteURL", SD.URL_FE);

                      
                        //_messageBus.SendMessage(new EmailLogger()
                        //{
                        //    UserId = tutor.Id,
                        //    Email = tutor.Email,
                        //    Subject = subject,
                        //    Message = htmlMessage
                        //}, queueName);
                        await _messageBus.SendEmailAsync(tutor.Email, subject, htmlMessage);
                        var connectionId = NotificationHub.GetConnectionIdByUserId(tutor.Id);
                        var notfication = new Notification()
                        {
                            ReceiverId = tutor.Id,
                            Message = _resourceService.GetString(SD.CHANGE_STATUS_CERTIFICATE_TUTOR_NOTIFICATION, SD.STATUS_APPROVE_VIE),
                            UrlDetail = string.Concat(SD.URL_FE, SD.URL_FE_TUTOR_SETTING),
                            IsRead = false,
                            CreatedDate = DateTime.Now
                        };
                        var notificationResult = await _notificationRepository.CreateAsync(notfication);
                        if (!string.IsNullOrEmpty(connectionId))
                        {
                            await _hubContext.Clients.Client(connectionId).SendAsync($"Notifications-{tutor.Id}", _mapper.Map<NotificationDTO>(notificationResult));
                        }
                    }

                    _response.Result = _mapper.Map<CertificateDTO>(model);
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    return Ok(_response);
                }
                else if (changeStatusDTO.StatusChange == (int)Status.REJECT)
                {
                    // Handle for reject
                    model.RejectionReason = changeStatusDTO.RejectionReason;
                    model.RequestStatus = Status.REJECT;
                    model.UpdatedDate = DateTime.Now;
                    model.ApprovedId = userId;
                    await _certificateRepository.UpdateAsync(model);

                    //Send mail
                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ChangeStatusTemplate.cshtml");
                    if (System.IO.File.Exists(templatePath) && tutor != null)
                    {
                        var subject = "Yêu cập nhật chứng chỉ của bạn đã bị từ chối!";
                        var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);

                        var rejectionReasonHtml = $"<p><strong>Lý do từ chối:</strong> {changeStatusDTO.RejectionReason}</p>";
                        var htmlMessage = templateContent
                            .Replace("@Model.FullName", tutor.FullName)
                            .Replace("@Model.IssueName", $"Yêu cầu cập nhật chứng chỉ {model.CertificateName} của bạn")
                            .Replace("@Model.IsApprovedString", "Từ chối")
                            .Replace("@Model.RejectionReason", rejectionReasonHtml)
                            .Replace("@Model.WebsiteURL", SD.URL_FE)
                            .Replace("@Model.Mail", SD.MAIL)
                            .Replace("@Model.Phone", SD.PHONE_NUMBER);
                       
                        //_messageBus.SendMessage(new EmailLogger()
                        //{
                        //    UserId = tutor.Id,
                        //    Email = tutor.Email,
                        //    Subject = subject,
                        //    Message = htmlMessage
                        //}, queueName);
                        await _messageBus.SendEmailAsync(tutor.Email, subject, htmlMessage);
                        var connectionId = NotificationHub.GetConnectionIdByUserId(tutor.Id);
                        var notfication = new Notification()
                        {
                            ReceiverId = tutor.Id,
                            Message = _resourceService.GetString(SD.CHANGE_STATUS_CERTIFICATE_TUTOR_NOTIFICATION, SD.STATUS_REJECT_VIE),
                            UrlDetail = string.Concat(SD.URL_FE, SD.URL_FE_TUTOR_SETTING),
                            IsRead = false,
                            CreatedDate = DateTime.Now
                        };
                        var notificationResult = await _notificationRepository.CreateAsync(notfication);
                        if (!string.IsNullOrEmpty(connectionId))
                        {
                            await _hubContext.Clients.Client(connectionId).SendAsync($"Notifications-{tutor.Id}", _mapper.Map<NotificationDTO>(notificationResult));
                        }
                    }
                    _response.Result = _mapper.Map<CertificateDTO>(model);
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    return Ok(_response);
                }
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpGet]
        [Authorize(Roles = $"{SD.STAFF_ROLE},{MANAGER_ROLE},{TUTOR_ROLE}")]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? search, string? status = SD.STATUS_ALL, string? orderBy = SD.CREATED_DATE, string? sort = SD.ORDER_DESC, int pageNumber = 1)
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
                if (userRoles == null || (!userRoles.Contains(SD.MANAGER_ROLE) && !userRoles.Contains(SD.TUTOR_ROLE) && !userRoles.Contains(SD.STAFF_ROLE)))
                {
                   
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }

                Expression<Func<Certificate, bool>> filter = u => u.SubmitterId != null;
                Expression<Func<Certificate, object>> orderByQuery = u => true;

                if (userRoles.Contains(SD.TUTOR_ROLE))
                {
                    filter = u => !string.IsNullOrEmpty(u.SubmitterId) && u.SubmitterId == userId && !u.IsDeleted;
                }
                if (search != null && !string.IsNullOrEmpty(search))
                {
                    filter = filter.AndAlso(x => x.CertificateName.ToLower().Contains(search.ToLower()));
                }
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
                else
                {
                    orderByQuery = x => x.CreatedDate;
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

                var (count, result) = await _certificateRepository.GetAllAsync(
                    filter,
                    "Submitter,CertificateMedias",
                    pageSize: 5,
                    pageNumber: pageNumber,
                    orderByQuery,
                    isDesc
                );

                foreach (var item in result)
                {
                    if (item.Submitter != null)
                    {
                        item.Submitter.User = await _userRepository.GetAsync(u => u.Id == item.SubmitterId, false, null);
                    }
                }
                // Setup pagination and response
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = 5, Total = count };
                _response.Result = _mapper.Map<List<CertificateDTO>>(result);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpDelete("{id:int}", Name = "DeleteCertificate")]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> DeleteAsync(int id)
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
            if (userRoles == null || (!userRoles.Contains(SD.TUTOR_ROLE)))
            {
               
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.Forbidden;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                return StatusCode((int)HttpStatusCode.Forbidden, _response);
            }

            try
            {
                if (id <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                var model = await _certificateRepository.GetAsync(x => x.Id == id && x.SubmitterId == userId, false, null);

                if (model == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.CERTIFICATE) };
                    return NotFound(_response);
                }
                model.IsDeleted = true;
                await _certificateRepository.UpdateAsync(model);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);

            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

    }
}
