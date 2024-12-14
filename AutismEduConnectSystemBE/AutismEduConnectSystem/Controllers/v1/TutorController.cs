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

namespace AutismEduConnectSystem.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class TutorController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ITutorRepository _tutorRepository;
        private readonly ITutorRequestRepository _tutorRequestRepository;
        private readonly ITutorProfileUpdateRequestRepository _tutorProfileUpdateRequestRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<TutorController> _logger;
        private readonly FormatString _formatString;
        protected APIResponse _response;
        protected int pageSize = 0;
        private readonly IResourceService _resourceService;
        private readonly IEmailSender _messageBus;
        private string queueName = string.Empty;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly INotificationRepository _notificationRepository;

        public TutorController(IUserRepository userRepository, ITutorRepository tutorRepository,
            IMapper mapper, IConfiguration configuration,
            FormatString formatString, ITutorProfileUpdateRequestRepository tutorProfileUpdateRequestRepository,
            ITutorRequestRepository tutorRequestRepository, IResourceService resourceService, ILogger<TutorController> logger,
            IHubContext<NotificationHub> hubContext, INotificationRepository notificationRepository,
            IEmailSender messageBus)
        {
            _messageBus = messageBus;
            _notificationRepository = notificationRepository;
            _tutorProfileUpdateRequestRepository = tutorProfileUpdateRequestRepository;
            _formatString = formatString;
            queueName = configuration["RabbitMQSettings:QueueName"];
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _response = new APIResponse();
            _mapper = mapper;
            _userRepository = userRepository;
            _tutorRepository = tutorRepository;
            _tutorRequestRepository = tutorRequestRepository;
            _resourceService = resourceService;
            _logger = logger;
            _hubContext = hubContext;
        }

        [HttpGet("updateRequest")]
        [Authorize(Roles = $"{SD.TUTOR_ROLE},{SD.STAFF_ROLE},{SD.MANAGER_ROLE}")]
        public async Task<ActionResult<APIResponse>> GetAllUpdateRequestProfileAsync([FromQuery] string? search, string? status = SD.STATUS_ALL, string? orderBy = SD.CREATED_DATE, string? sort = SD.ORDER_DESC, int pageNumber = 1)
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
                Expression<Func<TutorProfileUpdateRequest, bool>> filter = u => true;
                Expression<Func<TutorProfileUpdateRequest, bool>> filterName = null;
                Expression<Func<TutorProfileUpdateRequest, object>> orderByQuery = u => true;

                if (userRoles != null && userRoles.Contains(SD.TUTOR_ROLE))
                {
                    filter = filter.AndAlso(u => u.TutorId == userId);
                }

                if (!string.IsNullOrEmpty(search))
                {
                    filterName = x => x.Tutor.User != null && !string.IsNullOrEmpty(x.Tutor.User.FullName) && !string.IsNullOrEmpty(x.Tutor.User.Email)
                    && (x.Tutor.User.FullName.ToLower().Contains(search.ToLower()) || x.Tutor.User.Email.ToLower().Contains(search.ToLower()));
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
                var (count, result) = await _tutorProfileUpdateRequestRepository.GetAllTutorUpdateRequestAsync
                    (filterName: filterName, filterOther: filter, includeProperties: null, pageSize: 5, pageNumber: pageNumber, orderBy: orderByQuery, isDesc: isDesc);
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = 5, Total = count };
                _response.Result = _mapper.Map<List<TutorProfileUpdateRequestDTO>>(result);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving update request profiles for TutorId={TutorId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet("profile")]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> GetProfileTutor()
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

                var (totalPending, pendingRequests) = await _tutorProfileUpdateRequestRepository.GetAllNotPagingAsync(
                        x => x.TutorId == userId && x.RequestStatus == Status.PENDING,
                        null,
                        null
                    );

                var latestRequest = pendingRequests
                    .OrderByDescending(x => x.CreatedDate)
                    .FirstOrDefault();

                if (latestRequest == null)
                {
                    var (totalApproved, approvedRequests) = await _tutorProfileUpdateRequestRepository.GetAllNotPagingAsync(
                        x => x.TutorId == userId && x.RequestStatus == Status.APPROVE,
                        null,
                        null
                    );

                    latestRequest = approvedRequests
                        .OrderByDescending(x => x.CreatedDate)
                        .FirstOrDefault();
                }
                if (latestRequest != null)
                {
                    _response.Result = _mapper.Map<TutorProfileUpdateRequestDTO>(latestRequest);
                }
                else
                {
                    var tutor = await _tutorRepository.GetAsync(
                        x => x.TutorId == userId,
                        false,
                        "User",
                        null
                    );
                    if (tutor != null)
                    {
                        _response.Result = _mapper.Map<TutorDTO>(tutor);
                    }
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the profile for TutorId={TutorId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse>> GetByIdAsync(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                   Console.WriteLine("Bad request. Missing or invalid TutorId.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                var requests = new List<int>();
                if (userRoles != null && userRoles.Contains(SD.PARENT_ROLE))
                {
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId))
                    {
                        
                        _response.IsSuccess = false;
                        _response.StatusCode = HttpStatusCode.Unauthorized;
                        _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                        return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                    }
                    var parent = await _userRepository.GetAsync(x => x.Id == userId, false, "TutorRequests");
                    var (total, listRequests) = await _tutorRequestRepository.GetAllNotPagingAsync(x => x.TutorId == id && x.ParentId == userId && x.RejectType == RejectType.IncompatibilityWithCurriculum, null, null);
                    requests = listRequests.Select(x => x.ChildId).ToList();
                }

                Tutor model = await _tutorRepository.GetAsync(x => x.TutorId == id, false, "User,Curriculums,AvailableTimeSlots,Certificates,WorkExperiences,Reviews");
                if (model == null)
                {
                   Console.WriteLine("Tutor with TutorId={TutorId} not found", id);
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.TUTOR) };
                    return NotFound(_response);
                }
                model.TotalReview = model.Reviews.Where(x => !x.IsHide).ToList().Count;
                //model.ReviewScore = model.Reviews != null && model.Reviews.Any() ? model.Reviews.Where(x => !x.IsHide).ToList().Average(x => x.RateScore) : 5;

                if(model.Reviews != null && model.Reviews.Any() && model.Reviews.Where(x => !x.IsHide).ToList().Any())
                {
                    model.ReviewScore = model.Reviews.Where(x => !x.IsHide).ToList().Average(x => x.RateScore);
                }
                else
                {
                    model.ReviewScore = 5;
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
                _logger.LogError(ex, "An error occurred while retrieving tutor details for TutorId={TutorId}", id);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPut("{id}")]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> UpdateProfileAsync(TutorProfileUpdateRequestCreateDTO updateDTO)
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
                if (!ModelState.IsValid) 
                {
                   Console.WriteLine("Duplicated Request.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.UPDATE_PROFILE_REQUEST) };
                    return StatusCode((int)HttpStatusCode.BadRequest, _response);
                }
                var existedRequest = await _tutorProfileUpdateRequestRepository.GetAllNotPagingAsync(x => x.TutorId == userId && x.RequestStatus == Status.PENDING, null, null, x => x.CreatedDate, true);
                if(existedRequest.list != null && existedRequest.list.Any())
                {
                   Console.WriteLine("Duplicated Request.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.IN_STATUS_PENDING, SD.UPDATE_PROFILE_REQUEST) };
                    return StatusCode((int)HttpStatusCode.BadRequest, _response);
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
                _logger.LogError(ex, "An error occurred while updating the TutorProfile for TutorId={TutorId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? search, string? searchAddress, int? reviewScore = 5, int? ageFrom = 0, int? ageTo = 15, int pageNumber = 1)
        {
            try
            {
                if (ageFrom == null || ageTo == null || ageFrom < 0 || ageTo <= 0)
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
                if (reviewScore < 0) reviewScore = 5;
                Expression<Func<Tutor, bool>> filterAge = u => u.StartAge <= ageTo && u.EndAge >= ageFrom;
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
                    filterAge: filterAge, includeProperties: "User", pageSize: 9, pageNumber: pageNumber, orderBy: null, isDesc: true);
                List<TutorDTO> tutorDTOList = _mapper.Map<List<TutorDTO>>(result.OrderByDescending(x => x.ReviewScore).ToList());
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = 9, Total = count };
                _response.Result = tutorDTOList;
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching tutors.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPut("changeStatus/{id}")]
        [Authorize(Roles = $"{SD.STAFF_ROLE},{SD.MANAGER_ROLE}")]
        public async Task<ActionResult<APIResponse>> UpdateStatusRequest(ChangeStatusDTO changeStatusDTO)
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
                TutorProfileUpdateRequest model = await _tutorProfileUpdateRequestRepository.GetAsync(x => x.Id == changeStatusDTO.Id, false, null, null);
                if (model == null)
                {
                   Console.WriteLine("Duplicated Request.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.UPDATE_PROFILE_REQUEST) };
                    return StatusCode((int)HttpStatusCode.NotFound, _response);
                }
                var tutor = await _userRepository.GetAsync(x => x.Id == model.TutorId, true, null);
                var tutorProfile = await _tutorRepository.GetAsync(x => x.TutorId == model.TutorId, true, null);
                if (changeStatusDTO.StatusChange == (int)Status.APPROVE && tutor != null && tutorProfile != null)
                {
                    model.RequestStatus = Status.APPROVE;
                    model.UpdatedDate = DateTime.Now;
                    model.ApprovedId = userId;
                    await _tutorProfileUpdateRequestRepository.UpdateAsync(model);

                    tutorProfile.AboutMe = model.AboutMe;
                    tutorProfile.PriceFrom = model.PriceFrom;
                    tutorProfile.PriceEnd = model.PriceEnd;
                    tutorProfile.SessionHours = model.SessionHours;
                    tutorProfile.StartAge = model.StartAge;
                    tutorProfile.EndAge = model.EndAge;
                    tutor.PhoneNumber = model.PhoneNumber;
                    tutor.Address = model.Address;
                    tutorProfile.UpdatedDate = DateTime.Now;

                    await _tutorRepository.UpdateAsync(tutorProfile);
                    await _userRepository.UpdateAsync(tutor);
                    tutorProfile.User = tutor;
                    model.Tutor = tutorProfile;
                    // Send mail
                    var subject = "Yêu cập nhật thông tin của bạn đã được chấp nhận!";
                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ChangeStatusTemplate.cshtml");
                    if (System.IO.File.Exists(templatePath))
                    {
                        var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);

                        var rejectionReasonHtml = string.Empty;
                        var htmlMessage = templateContent
                        .Replace("@Model.FullName", tutor.FullName)
                        .Replace("@Model.IssueName", $"Yêu cầu cập nhật thông tin của bạn")
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
                    }
                    var connectionId = NotificationHub.GetConnectionIdByUserId(tutor.Id);
                    var notfication = new Notification()
                    {
                        ReceiverId = tutor.Id,
                        Message = _resourceService.GetString(SD.CHANGE_STATUS_PROFILE_TUTOR_NOTIFICATION, SD.STATUS_APPROVE_VIE),
                        UrlDetail = string.Concat(SD.URL_FE, SD.URL_FE_TUTOR_SETTING),
                        IsRead = false,
                        CreatedDate = DateTime.Now
                    };
                    var notificationResult = await _notificationRepository.CreateAsync(notfication);
                    if (!string.IsNullOrEmpty(connectionId))
                    {
                        await _hubContext.Clients.Client(connectionId).SendAsync($"Notifications-{tutor.Id}", _mapper.Map<NotificationDTO>(notificationResult));
                    }
                    _response.Result = _mapper.Map<TutorProfileUpdateRequestDTO>(model);
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    return Ok(_response);
                }
                else if (changeStatusDTO.StatusChange == (int)Status.REJECT && tutor != null && tutorProfile != null)
                {
                    // Handle for reject
                    model.RejectionReason = changeStatusDTO.RejectionReason;
                    model.RequestStatus = Status.REJECT;
                    model.UpdatedDate = DateTime.Now;
                    model.ApprovedId = userId;
                    await _tutorProfileUpdateRequestRepository.UpdateAsync(model);
                    tutorProfile.User = tutor;
                    model.Tutor = tutorProfile;
                    //Send mail
                    var subject = "Yêu cập nhật thông tin của bạn đã bị từ chối!";
                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ChangeStatusTemplate.cshtml");
                    if (System.IO.File.Exists(templatePath))
                    {
                        var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);

                        var rejectionReasonHtml = $"<p><strong>Lý do từ chối:</strong> {changeStatusDTO.RejectionReason}</p>";
                        var htmlMessage = templateContent
                            .Replace("@Model.FullName", tutor.FullName)
                            .Replace("@Model.IssueName", $"Yêu cầu cập nhật thông tin của bạn")
                            .Replace("@Model.IsApprovedString", "Từ chối")
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
                    }
                    var connectionId = NotificationHub.GetConnectionIdByUserId(tutor.Id);
                    var notfication = new Notification()
                    {
                        ReceiverId = tutor.Id,
                        Message = _resourceService.GetString(SD.CHANGE_STATUS_PROFILE_TUTOR_NOTIFICATION, SD.STATUS_REJECT_VIE),
                        UrlDetail = string.Concat(SD.URL_FE, SD.URL_FE_TUTOR_SETTING),
                        IsRead = false,
                        CreatedDate = DateTime.Now
                    };
                    var notificationResult = await _notificationRepository.CreateAsync(notfication);
                    if (!string.IsNullOrEmpty(connectionId))
                    {
                        await _hubContext.Clients.Client(connectionId).SendAsync($"Notifications-{tutor.Id}", _mapper.Map<NotificationDTO>(notificationResult));
                    }
                    _response.Result = _mapper.Map<TutorProfileUpdateRequestDTO>(model);
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
                _logger.LogError(ex, "An error occurred while changing the status of certificate ID {CertificateId} by user {UserId}", changeStatusDTO.Id, userId);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
