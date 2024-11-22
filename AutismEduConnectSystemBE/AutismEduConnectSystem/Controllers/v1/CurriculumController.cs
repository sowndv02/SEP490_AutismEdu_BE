using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Models.DTOs;
using AutismEduConnectSystem.Models.DTOs.CreateDTOs;
using AutismEduConnectSystem.Models.DTOs.UpdateDTOs;
using AutismEduConnectSystem.RabbitMQSender;
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

namespace AutismEduConnectSystem.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class CurriculumController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ITutorRepository _tutorRepository;
        private readonly ICurriculumRepository _curriculumRepository;
        private readonly IMapper _mapper;
        private string queueName = string.Empty;
        private readonly IRabbitMQMessageSender _messageBus;
        private readonly ILogger<CurriculumController> _logger;
        protected APIResponse _response;
        private readonly IResourceService _resourceService;
        private readonly INotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationHub> _hubContext;

        public CurriculumController(IUserRepository userRepository, ITutorRepository tutorRepository,
            IMapper mapper, IConfiguration configuration, ILogger<CurriculumController> logger,
            ICurriculumRepository curriculumRepository, IRabbitMQMessageSender messageBus, IResourceService resourceService,
            INotificationRepository notificationRepository, IHubContext<NotificationHub> hubContext)
        {
            _logger = logger;
            _messageBus = messageBus;
            _curriculumRepository = curriculumRepository;
            queueName = configuration["RabbitMQSettings:QueueName"];
            _response = new APIResponse();
            _mapper = mapper;
            _userRepository = userRepository;
            _tutorRepository = tutorRepository;
            _resourceService = resourceService;
            _notificationRepository = notificationRepository;
            _hubContext = hubContext;
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> DeleteAsync(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                if (userRoles == null || (!userRoles.Contains(SD.TUTOR_ROLE)))
                {
                    _logger.LogWarning("Forbidden access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }

                if (id <= 0)
                {
                    _logger.LogWarning("Invalid curriculum ID: {CurriculumId}. Returning BadRequest.", id);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                var model = await _curriculumRepository.GetAsync(x => x.Id == id && x.SubmitterId == userId, true, null, null);

                if (model == null)
                {
                    _logger.LogWarning("Curriculum not found for ID: {CurriculumId} and User ID: {UserId}. Returning BadRequest.", id, userId);
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.CURRICULUM) };
                    return NotFound(_response);
                }
                model.IsActive = false;
                model.IsDeleted = true;
                await _curriculumRepository.UpdateAsync(model);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting curriculum ID: {CurriculumId}", id);
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return _response;
            }

        }

        [HttpGet]
        [Authorize(Roles = $"{SD.TUTOR_ROLE},{SD.STAFF_ROLE},{SD.MANAGER_ROLE}")]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? search, string? status = SD.STATUS_ALL, int pageSize = 0, string? orderBy = SD.CREATED_DATE, string? sort = SD.ORDER_DESC, int pageNumber = 1)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                if (userRoles == null || (!userRoles.Contains(SD.MANAGER_ROLE) && !userRoles.Contains(SD.TUTOR_ROLE) && !userRoles.Contains(SD.STAFF_ROLE)))
                {
                    _logger.LogWarning("Forbidden access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }

                int totalCount = 0;
                List<Curriculum> list = new();
                Expression<Func<Curriculum, bool>> filter = u => true;

                if (userRoles.Contains(SD.TUTOR_ROLE))
                {
                    filter = filter.AndAlso(u => !string.IsNullOrEmpty(u.SubmitterId) && u.SubmitterId == userId && !u.IsDeleted);
                }
                if (search != null)
                {
                    filter = filter.AndAlso(u => u.Description.Contains(search));
                }
                bool isDesc = !string.IsNullOrEmpty(sort) && sort == SD.ORDER_DESC;
                Expression<Func<Curriculum, object>>? orderByQuery = null;

                if (orderBy != null)
                {
                    switch (orderBy)
                    {
                        case SD.CREATED_DATE:
                            orderByQuery = x => x.CreatedDate;
                            break;
                        case SD.AGE_FROM:
                            orderByQuery = x => x.AgeFrom;
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
                if (pageSize != 0)
                {
                    var (count, result) = await _curriculumRepository.GetAllAsync(filter,
                                "Submitter,TutorRegistrationRequest", pageSize: pageSize, pageNumber: pageNumber, orderByQuery, isDesc);
                    list = result;
                    totalCount = count;
                }
                else
                {
                    var (count, result) = await _curriculumRepository.GetAllNotPagingAsync(filter,
                                "Submitter,TutorRegistrationRequest", null, orderByQuery, isDesc);
                    list = result;
                    totalCount = count;
                }
                foreach (var item in list)
                {
                    if (item.Submitter != null)
                    {
                        item.Submitter.User = await _userRepository.GetAsync(u => u.Id == item.SubmitterId, false, null);
                    }
                }
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = totalCount };
                _response.Result = _mapper.Map<List<CurriculumDTO>>(list);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching curricula for user ID: {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> CreateAsync(CurriculumCreateDTO curriculumDto)
        {
            try
            {

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                if (userRoles == null || (!userRoles.Contains(SD.TUTOR_ROLE)))
                {
                    _logger.LogWarning("Forbidden access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for CreateAsync method. Model state errors: {@ModelState}", ModelState);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.CURRICULUM) };
                    return BadRequest(_response);
                }

                var isExistedCurriculum = await _curriculumRepository.GetAllNotPagingAsync(x => x.OriginalCurriculumId == curriculumDto.OriginalCurriculumId && x.RequestStatus == SD.Status.PENDING, null, null, null, true);
                if (isExistedCurriculum.TotalCount > 0)
                {
                    _logger.LogWarning("Cannot spam update curriculum");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.CURRICULUM) };
                    return BadRequest(_response);
                }

                if (curriculumDto.OriginalCurriculumId == 0)
                {
                    var (total, list) = await _curriculumRepository.GetAllNotPagingAsync(x => x.AgeFrom <= curriculumDto.AgeFrom && curriculumDto.AgeEnd >= x.AgeEnd && x.SubmitterId == userId && !x.IsDeleted && x.IsActive, null, null, null, false);
                    foreach (var item in list)
                    {
                        if (item.AgeFrom == curriculumDto.AgeFrom && item.AgeEnd == curriculumDto.AgeEnd)
                        {
                            _logger.LogWarning("Duplicate age range found for AgeFrom: {AgeFrom} and AgeEnd: {AgeEnd}", curriculumDto.AgeFrom, curriculumDto.AgeEnd);
                            _response.StatusCode = HttpStatusCode.BadRequest;
                            _response.IsSuccess = false;
                            _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.AGE) };
                            return BadRequest(_response);
                        }
                    }
                }

                var newCurriculum = _mapper.Map<Curriculum>(curriculumDto);

                newCurriculum.SubmitterId = userId;
                newCurriculum.IsActive = false;
                newCurriculum.VersionNumber = await _curriculumRepository.GetNextVersionNumberAsync(curriculumDto.OriginalCurriculumId);
                if (curriculumDto.OriginalCurriculumId == 0)
                {
                    newCurriculum.OriginalCurriculumId = null;
                }
                await _curriculumRepository.CreateAsync(newCurriculum);
                _response.StatusCode = HttpStatusCode.Created;
                _response.Result = _mapper.Map<CurriculumDTO>(newCurriculum);
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating curriculum for user ID: {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
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
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                if (userRoles == null || (!userRoles.Contains(SD.STAFF_ROLE) && !userRoles.Contains(SD.MANAGER_ROLE) && !userRoles.Contains(SD.TUTOR_ROLE)))
                {
                    _logger.LogWarning("Forbidden access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }

                Curriculum model = await _curriculumRepository.GetAsync(x => x.Id == changeStatusDTO.Id, true, "Submitter", null);
                var tutor = await _userRepository.GetAsync(x => x.Id == model.SubmitterId, false, null);
                if (model == null || model.RequestStatus != Status.PENDING)
                {
                    _logger.LogWarning("Curriculum not found for ID: {CurriculumId}", changeStatusDTO.Id);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.CURRICULUM) };
                    return BadRequest(_response);
                }
                if (model.RequestStatus != Status.PENDING)
                {
                    _logger.LogWarning("Curriculum ID: {CurriculumId} has already been processed with status: {RequestStatus}", changeStatusDTO.Id, model.RequestStatus);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.CURRICULUM) };
                    return BadRequest(_response);
                }
                if (changeStatusDTO.StatusChange == (int)Status.APPROVE)
                {
                    model.RequestStatus = Status.APPROVE;
                    model.UpdatedDate = DateTime.Now;
                    model.IsActive = true;
                    model.ApprovedId = userId;
                    await _curriculumRepository.DeactivatePreviousVersionsAsync(model.OriginalCurriculumId);
                    await _curriculumRepository.UpdateAsync(model);
                    // Send mail
                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ChangeStatusTemplate.cshtml");
                    if (System.IO.File.Exists(templatePath) && tutor != null)
                    {
                        var subject = "Yêu cập nhật khung chương trình của bạn đã được chấp nhận!";
                        var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);
                        
                        var rejectionReasonHtml = string.Empty;
                        var htmlMessage = templateContent
                        .Replace("@Model.FullName", tutor.FullName)
                        .Replace("@Model.IssueName", $"Yêu cầu cập nhật khung chương trình của bạn")
                        .Replace("@Model.IsApprovedString", "Chấp nhận")
                        .Replace("@Model.RejectionReason", rejectionReasonHtml);

                        _messageBus.SendMessage(new EmailLogger()
                        {
                            UserId = tutor.Id,
                            Email = tutor.Email,
                            Message = htmlMessage,
                            Subject = subject
                        }, queueName);
                        var connectionId = NotificationHub.GetConnectionIdByUserId(tutor.Id);
                        var notfication = new Notification()
                        {
                            ReceiverId = tutor.Id,
                            Message = _resourceService.GetString(SD.CHANGE_STATUS_CURRICULUM_TUTOR_NOTIFICATION, SD.STATUS_APPROVE_VIE),
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

                    _response.Result = _mapper.Map<CurriculumDTO>(model);
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
                    await _curriculumRepository.UpdateAsync(model);
                    // Send mail
                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ChangeStatusTemplate.cshtml");
                    if (System.IO.File.Exists(templatePath) && tutor != null)
                    {
                        var subject = "Yêu cập nhật khung chương trình của bạn đã bị từ chối!";
                        var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);

                        var rejectionReasonHtml = $"<p><strong>Lý do từ chối:</strong> {changeStatusDTO.RejectionReason}</p>";
                        var htmlMessage = templateContent
                            .Replace("@Model.FullName", tutor.FullName)
                            .Replace("@Model.IssueName", $"Yêu cầu cập nhật khung chương trình của bạn")
                            .Replace("@Model.IsApprovedString", "Từ chối")
                            .Replace("@Model.RejectionReason", rejectionReasonHtml);

                        _messageBus.SendMessage(new EmailLogger()
                        {
                            UserId = tutor.Id,
                            Email = tutor.Email,
                            Subject = subject,
                            Message = htmlMessage
                        }, queueName);
                        var connectionId = NotificationHub.GetConnectionIdByUserId(tutor.Id);
                        var notfication = new Notification()
                        {
                            ReceiverId = tutor.Id,
                            Message = _resourceService.GetString(SD.CHANGE_STATUS_CURRICULUM_TUTOR_NOTIFICATION, SD.STATUS_REJECT_VIE),
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
                    _response.Result = _mapper.Map<CurriculumDTO>(model);
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
                _logger.LogError(ex, "Error occurred while approving or rejecting curriculum request for curriculum ID: {CurriculumId}", changeStatusDTO.Id);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
