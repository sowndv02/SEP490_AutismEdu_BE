using AutismEduConnectSystem.DTOs;
using AutismEduConnectSystem.DTOs.CreateDTOs;
using AutismEduConnectSystem.DTOs.UpdateDTOs;
using AutismEduConnectSystem.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.SignalR;
using AutismEduConnectSystem.Utils;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using static AutismEduConnectSystem.SD;
using Azure.Core;

namespace AutismEduConnectSystem.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class TutorRequestController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ITutorRequestRepository _tutorRequestRepository;
        private readonly IChildInformationRepository _childInformationRepository;
        private readonly IMapper _mapper;
        private string queueName = string.Empty;
        private readonly ILogger<TutorRequestController> _logger;
        private readonly IEmailSender _messageBus;
        protected APIResponse _response;
        protected int pageSize = 0;
        private readonly IResourceService _resourceService;
        private readonly INotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationHub> _hubContext;

        public TutorRequestController(IUserRepository userRepository, ITutorRequestRepository tutorRequestRepository,
            IMapper mapper, IConfiguration configuration,
            IEmailSender messageBus, IResourceService resourceService, ILogger<TutorRequestController> logger,
            INotificationRepository notificationRepository, IHubContext<NotificationHub> hubContext, IChildInformationRepository childInformationRepository)
        {
            _messageBus = messageBus;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            queueName = configuration["RabbitMQSettings:QueueName"];
            _response = new APIResponse();
            _mapper = mapper;
            _userRepository = userRepository;
            _tutorRequestRepository = tutorRequestRepository;
            _resourceService = resourceService;
            _logger = logger;
            _notificationRepository = notificationRepository;
            _hubContext = hubContext;
            _childInformationRepository = childInformationRepository;
        }


        [HttpGet("NoStudentProfile")]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> GetAllRequestNoStudentProfileAsync(string? orderBy = SD.CREATED_DATE, string? sort = SD.ORDER_DESC)
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
                if (userRoles == null || (!userRoles.Contains(SD.TUTOR_ROLE)))
                {
                   
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                Expression<Func<TutorRequest, bool>> filter = u => u.TutorId == userId && u.RequestStatus == SD.Status.APPROVE && !u.HasStudentProfile;
                Expression<Func<TutorRequest, object>> orderByQuery = u => true;
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
                var (count, result) = await _tutorRequestRepository.GetAllNotPagingAsync(filter,
                               includeProperties: "Parent,ChildInformation", excludeProperties: null, orderBy: orderByQuery, isDesc);
                _response.Result = _mapper.Map<List<TutorRequestDTO>>(result);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = null;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request for TutorRequests with status.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet("history")]
        [Authorize(Roles = SD.PARENT_ROLE)]
        public async Task<ActionResult<APIResponse>> GetAllHistoryRequestAsync([FromQuery] string? status = SD.STATUS_ALL, string? orderBy = SD.CREATED_DATE, string? sort = SD.ORDER_DESC, int pageNumber = 1)
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
                if (userRoles == null || (!userRoles.Contains(SD.PARENT_ROLE)))
                {
                   
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                
                Expression<Func<TutorRequest, bool>> filter = u => u.ParentId == userId;
                Expression<Func<TutorRequest, object>> orderByQuery = u => true;

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
                var (count, result) = await _tutorRequestRepository.GetAllWithIncludeAsync(filter,
                               "Tutor,ChildInformation", pageSize: 5, pageNumber: pageNumber, orderByQuery, isDesc);
                foreach (var item in result)
                {
                    if(item.Tutor != null)
                    {
                        item.Tutor.User = await _userRepository.GetAsync(x => x.Id == item.TutorId);
                    }
                }
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = 5, Total = count };
                _response.Result = _mapper.Map<List<TutorRequestDTO>>(result);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the history request for user {UserId} with status {Status}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, status);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet]
        [Authorize(Roles = SD.TUTOR_ROLE)]
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
                if (userRoles == null || (!userRoles.Contains(SD.TUTOR_ROLE)))
                {
                   
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                Expression<Func<TutorRequest, bool>> filter = u => u.TutorId == userId;
                Expression<Func<TutorRequest, object>> orderByQuery = u => true;
                if (!string.IsNullOrEmpty(search))
                {
                    filter = filter.AndAlso(u => !string.IsNullOrEmpty(u.Parent.Email) && !string.IsNullOrEmpty(u.Parent.FullName) && (u.Parent.Email.ToLower().Contains(search.ToLower()) || u.Parent.FullName.ToLower().Contains(search.ToLower())));
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
                var (count, result) = await _tutorRequestRepository.GetAllWithIncludeAsync(filter,
                               "Parent,ChildInformation", pageSize: 5, pageNumber: pageNumber, orderByQuery, isDesc);

                Pagination pagination = new() { PageNumber = pageNumber, PageSize = 5, Total = count };
                _response.Result = _mapper.Map<List<TutorRequestDTO>>(result);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching TutorRequests for user {UserId} with search {Search}, status {Status}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, search, status);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost]
        [Authorize(Roles = SD.PARENT_ROLE)]
        public async Task<ActionResult<APIResponse>> CreateAsync(TutorRequestCreateDTO tutorRequestCreateDTO)
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
                if (userRoles == null || (!userRoles.Contains(SD.PARENT_ROLE)))
                {
                   
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
               
                if (!ModelState.IsValid)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.TUTOR_REQUEST) };
                    return BadRequest(_response);
                }
                if (tutorRequestCreateDTO.ChildId <= 0)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.CHILD) };
                    return NotFound(_response);
                }
                var isExistedRequest = await _tutorRequestRepository.GetAllNotPagingAsync(x => x.ParentId == userId && x.TutorId == tutorRequestCreateDTO.TutorId && x.ChildId == tutorRequestCreateDTO.ChildId && (x.RequestStatus == SD.Status.PENDING || x.RequestStatus == SD.Status.APPROVE));
                if (isExistedRequest.list != null && isExistedRequest.list.Any())
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.TUTOR_REQUEST) };
                    return BadRequest(_response);
                }
                var tutor = await _userRepository.GetAsync(x => x.Id == tutorRequestCreateDTO.TutorId);
                if (tutor == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.TUTOR) };
                    return NotFound(_response);
                }
                var child = await _childInformationRepository.GetAsync(x => x.Id == tutorRequestCreateDTO.ChildId && x.ParentId == userId);
                if (child == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.CHILD) };
                    return NotFound(_response);
                }
                TutorRequest model = _mapper.Map<TutorRequest>(tutorRequestCreateDTO);
                model.ParentId = userId;
                model.CreatedDate = DateTime.Now;
                var createdObject = await _tutorRequestRepository.CreateAsync(model);
                var parent = await _userRepository.GetAsync(x => x.Id == model.ParentId);

                // Send mail for parent
                var parentTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ParentRequestConfirmationTemplate.cshtml");
                if (System.IO.File.Exists(parentTemplatePath) && parent != null)
                {
                    var subjectForParent = "Xác nhận Yêu cầu Dạy học";
                    var parentTemplateContent = await System.IO.File.ReadAllTextAsync(parentTemplatePath);
                    var parentHtmlMessage = parentTemplateContent
                        .Replace("@Model.ParentFullName", parent.FullName)
                        .Replace("@Model.TutorFullName", tutor.FullName)
                        .Replace("@Model.TutorEmail", tutor.Email)
                        .Replace("@Model.TutorPhoneNumber", tutor.PhoneNumber)
                        .Replace("@Model.RequestDescription", model.Description)
                        .Replace("@Model.Mail", SD.MAIL)
                        .Replace("@Model.Phone", SD.PHONE_NUMBER)
                        .Replace("@Model.WebsiteURL", SD.URL_FE);
                   
                    //_messageBus.SendMessage(new EmailLogger()
                    //{
                    //    UserId = parent.Id,
                    //    Email = parent.Email,
                    //    Subject = subjectForParent,
                    //    Message = parentHtmlMessage
                    //}, queueName);
                    await _messageBus.SendEmailAsync(parent.Email, subjectForParent, parentHtmlMessage);
                }


                // Send mail for tutor
                var tutorTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "TutorRequestNotificationTemplate.cshtml");
                if (System.IO.File.Exists(parentTemplatePath) && tutor != null)
                {
                    var subjectForTutor = "Thông báo Yêu cầu Dạy học Mới";
                    var tutorTemplateContent = await System.IO.File.ReadAllTextAsync(tutorTemplatePath);
                    var tutorHtmlMessage = tutorTemplateContent
                        .Replace("@Model.TutorFullName", tutor.FullName)
                        .Replace("@Model.ParentFullName", parent.FullName)
                        .Replace("@Model.RequestDescription", model.Description)
                        .Replace("@Model.Mail", SD.MAIL)
                        .Replace("@Model.Phone", SD.PHONE_NUMBER)
                        .Replace("@Model.WebsiteURL", SD.URL_FE);
                
                    //_messageBus.SendMessage( new EmailLogger() 
                    //{ 
                    //    UserId = tutor.Id, 
                    //    Email = tutor.Email, 
                    //    Subject = subjectForTutor, 
                    //    Message = tutorHtmlMessage 
                    //}, queueName);
                    await _messageBus.SendEmailAsync(tutor.Email, subjectForTutor, tutorHtmlMessage);
                    // Notification
                    var connectionId = NotificationHub.GetConnectionIdByUserId(tutor.Id);
                    var notfication = new Notification()
                    {
                        ReceiverId = tutor.Id,
                        Message = _resourceService.GetString(SD.TUTOR_REQUEST_TUTOR_NOTIFICATION, parent.FullName),
                        UrlDetail = string.Concat(SD.URL_FE, SD.URL_FE_TUTOR_TUTOR_REQUEST),
                        IsRead = false,
                        CreatedDate = DateTime.Now
                    };
                    var notificationResult = await _notificationRepository.CreateAsync(notfication);
                    if (!string.IsNullOrEmpty(connectionId))
                    {
                        await _hubContext.Clients.Client(connectionId).SendAsync($"Notifications-{tutor.Id}", _mapper.Map<NotificationDTO>(notificationResult));
                    }
                }

                _response.Result = _mapper.Map<TutorRequestDTO>(createdObject);
                _response.StatusCode = HttpStatusCode.Created;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating a tutor request for ParentId {ParentId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPut("changeStatus/{id}")]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> UpdateStatusRequest(int id, ChangeStatusTutorRequestDTO changeStatusDTO)
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
                if (userRoles == null || (!userRoles.Contains(SD.TUTOR_ROLE)))
                {
                   
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                
                if (changeStatusDTO == null || !ModelState.IsValid || changeStatusDTO.StatusChange == (int)SD.Status.PENDING)
                {
                   Console.WriteLine($"Received a bad request for tutor request with ID {id}.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.TUTOR_REQUEST) };
                    return BadRequest(_response);
                }

                TutorRequest model = await _tutorRequestRepository.GetAsync(x => x.Id == changeStatusDTO.Id, false, "Parent,Tutor", null);
                if (model == null || model.RequestStatus != SD.Status.PENDING)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.TUTOR_REQUEST) };
                    return BadRequest(_response);
                }
                var tutor = await _userRepository.GetAsync(x => x.Id == model.TutorId, false, null);
                if (changeStatusDTO.StatusChange == (int)Status.APPROVE)
                {
                    model.RequestStatus = Status.APPROVE;
                    model.UpdatedDate = DateTime.Now;
                    model.RejectType = RejectType.Approved;
                    await _tutorRequestRepository.UpdateAsync(model);
                    // Send mail
                    var subject = $"Yêu cầu dạy học của bạn đến gia sư {tutor.FullName} đã được chấp nhận";
                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ChangeStatusTemplate.cshtml");
                    if (System.IO.File.Exists(templatePath) && model.Parent != null)
                    {
                        var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);

                        var rejectionReasonHtml = string.Empty;
                        var htmlMessage = templateContent
                            .Replace("@Model.FullName", model.Parent.FullName)
                            .Replace("@Model.IssueName", $"Yêu cầu dạy học của bạn đến gia sư {tutor.FullName}")
                            .Replace("@Model.IsApprovedString", "Chấp nhận")
                            .Replace("@Model.RejectionReason", rejectionReasonHtml)
                            .Replace("@Model.Mail", SD.MAIL)
                            .Replace("@Model.Phone", SD.PHONE_NUMBER)
                            .Replace("@Model.WebsiteURL", SD.URL_FE);

                        //_messageBus.SendMessage(new EmailLogger()
                        //{
                        //    UserId = model.ParentId,
                        //    Email = model.Parent.Email,
                        //    Subject = subject,
                        //    Message = htmlMessage
                        //}, queueName);
                        await _messageBus.SendEmailAsync(model.Parent.Email, subject, htmlMessage);
                        var connectionId = NotificationHub.GetConnectionIdByUserId(model.ParentId);
                        var notfication = new Notification()
                        {
                            ReceiverId = model.ParentId,
                            Message = _resourceService.GetString(SD.CHANGE_STATUS_TUTOR_REQUEST_PARENT_NOTIFICATION, SD.STATUS_APPROVE_VIE, tutor.FullName),
                            UrlDetail = string.Concat(SD.URL_FE, SD.URL_FE_PARENT_TUTOR_REQUEST),
                            IsRead = false,
                            CreatedDate = DateTime.Now
                        };
                        var notificationResult = await _notificationRepository.CreateAsync(notfication);
                        if (!string.IsNullOrEmpty(connectionId))
                        {
                            await _hubContext.Clients.Client(connectionId).SendAsync($"Notifications-{model.ParentId}", _mapper.Map<NotificationDTO>(notificationResult));
                        }
                    }
                    _response.Result = _mapper.Map<TutorRequestDTO>(model);
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    return Ok(_response);
                }
                else if (changeStatusDTO.StatusChange == (int)Status.REJECT)
                {
                    // Handle for reject
                    model.RejectionReason = changeStatusDTO.RejectionReason;
                    model.RequestStatus = Status.REJECT;
                    model.RejectType = changeStatusDTO.RejectType;
                    model.UpdatedDate = DateTime.Now;
                    var returnObject = await _tutorRequestRepository.UpdateAsync(model);
                    // Send mail
                    var reason = string.Empty;
                    switch (changeStatusDTO.RejectType)
                    {
                        case RejectType.SchedulingConflicts:
                            reason = SD.SchedulingConflictsMsg + "\n" + changeStatusDTO.RejectionReason;
                            break;
                        case RejectType.IncompatibilityWithCurriculum:
                            reason = SD.IncompatibilityWithCurriculumMsg + "\n" + changeStatusDTO.RejectionReason;
                            break;
                        case RejectType.Other:
                            reason = changeStatusDTO.RejectionReason;
                            break;
                        default:
                            model.RejectType = RejectType.Other;
                            reason = changeStatusDTO.RejectionReason;
                            break;
                    }
                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ChangeStatusTemplate.cshtml");
                    if (System.IO.File.Exists(templatePath) && model.Parent != null)
                    {
                        var subject = $"Yêu cầu dạy học của bạn đến gia sư {tutor.FullName} đã bị từ chối";
                        var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);
                        var rejectionReasonHtml = $"<p><strong>Lý do từ chối:</strong> {reason}</p>";
                        var htmlMessage = templateContent
                            .Replace("@Model.FullName", model.Parent.FullName)
                            .Replace("@Model.IssueName", $"Yêu cầu dạy học của bạn đến gia sư {tutor.FullName}")
                            .Replace("@Model.IsApprovedString", "Từ chối")
                            .Replace("@Model.RejectionReason", rejectionReasonHtml)
                            .Replace("@Model.Mail", SD.MAIL)
                            .Replace("@Model.Phone", SD.PHONE_NUMBER)
                            .Replace("@Model.WebsiteURL", SD.URL_FE);
                       
                        //_messageBus.SendMessage(new EmailLogger()
                        //{
                        //    UserId = model.ParentId,
                        //    Email = model.Parent.Email,
                        //    Subject = subject,
                        //    Message = htmlMessage
                        //}, queueName);
                        await _messageBus.SendEmailAsync(model.Parent.Email, subject, htmlMessage);
                        var connectionId = NotificationHub.GetConnectionIdByUserId(model.ParentId);
                        var notfication = new Notification()
                        {
                            ReceiverId = model.ParentId,
                            Message = _resourceService.GetString(SD.CHANGE_STATUS_TUTOR_REQUEST_PARENT_NOTIFICATION, SD.STATUS_REJECT_VIE, tutor.FullName),
                            UrlDetail = string.Concat(SD.URL_FE, SD.URL_FE_PARENT_TUTOR_REQUEST),
                            IsRead = false,
                            CreatedDate = DateTime.Now
                        };
                        var notificationResult = await _notificationRepository.CreateAsync(notfication);
                        if (!string.IsNullOrEmpty(connectionId))
                        {
                            await _hubContext.Clients.Client(connectionId).SendAsync($"Notifications-{model.ParentId}", _mapper.Map<NotificationDTO>(notificationResult));
                        }
                    }
                    _response.Result = _mapper.Map<TutorRequestDTO>(returnObject);
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

    }
}
