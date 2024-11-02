using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models.DTOs.UpdateDTOs;
using backend_api.RabbitMQSender;
using backend_api.Repository.IRepository;
using backend_api.Services.IServices;
using backend_api.Utils;
using Microsoft.AspNetCore.Authorization;

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
    public class TutorRequestController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ITutorRequestRepository _tutorRequestRepository;
        private readonly IMapper _mapper;
        private string queueName = string.Empty;
        private readonly IRabbitMQMessageSender _messageBus;
        protected APIResponse _response;
        protected int pageSize = 0;
        private readonly IResourceService _resourceService;

        public TutorRequestController(IUserRepository userRepository, ITutorRequestRepository tutorRequestRepository,
            IMapper mapper, IConfiguration configuration,
            IRabbitMQMessageSender messageBus, IResourceService resourceService)
        {
            _messageBus = messageBus;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            queueName = configuration.GetValue<string>("RabbitMQSettings:QueueName");
            _response = new APIResponse();
            _mapper = mapper;
            _userRepository = userRepository;
            _tutorRequestRepository = tutorRequestRepository;
            _resourceService = resourceService;
        }


        [HttpGet("NoStudentProfile")]
        public async Task<ActionResult<APIResponse>> GetAllRequestNoStudentProfileAsync(string? status = SD.STATUS_APPROVE, string? orderBy = SD.CREADTED_DATE, string? sort = SD.ORDER_DESC)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int totalCount = 0;
                List<TutorRequest> list = new();
                Expression<Func<TutorRequest, bool>> filter = u => u.TutorId == userId && u.RequestStatus == SD.Status.APPROVE && !u.HasStudentProfile;
                Expression<Func<TutorRequest, object>> orderByQuery = u => true;

                bool isDesc = !string.IsNullOrEmpty(sort) && sort == SD.ORDER_DESC;

                if (orderBy != null)
                {
                    switch (orderBy)
                    {
                        case SD.CREADTED_DATE:
                            orderByQuery = x => x.CreatedDate;
                            break;
                        default:
                            orderByQuery = x => x.CreatedDate;
                            break;
                    }
                }

                var (count, result) = await _tutorRequestRepository.GetAllNotPagingAsync(filter,
                               includeProperties: "Parent,ChildInformation", excludeProperties: null, orderBy: orderByQuery, isDesc);
                list = result;
                totalCount = count;

                _response.Result = _mapper.Map<List<TutorRequestDTO>>(list);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = null;
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

        [HttpGet("history")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllHistoryRequestAsync([FromQuery] string? status = SD.STATUS_ALL, string? orderBy = SD.CREADTED_DATE, string? sort = SD.ORDER_DESC, int pageNumber = 1)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int totalCount = 0;
                List<TutorRequest> list = new();
                Expression<Func<TutorRequest, bool>> filter = u => u.ParentId == userId;
                Expression<Func<TutorRequest, object>> orderByQuery = u => true;

                bool isDesc = !string.IsNullOrEmpty(sort) && sort == SD.ORDER_DESC;

                if (orderBy != null)
                {
                    switch (orderBy)
                    {
                        case SD.CREADTED_DATE:
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
                var (count, result) = await _tutorRequestRepository.GetAllWithIncludeAsync(filter,
                               "Tutor,ChildInformation", pageSize: 5, pageNumber: pageNumber, orderByQuery, isDesc);
                list = result;
                totalCount = count;
                foreach (var item in list)
                {
                    item.Tutor.User = await _userRepository.GetAsync(x => x.Id == item.TutorId);
                }
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = 5, Total = totalCount };
                _response.Result = _mapper.Map<List<TutorRequestDTO>>(list);
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

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? search, string? status = SD.STATUS_ALL, string? orderBy = SD.CREADTED_DATE, string? sort = SD.ORDER_DESC, int pageNumber = 1)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int totalCount = 0;
                List<TutorRequest> list = new();
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
                        case SD.CREADTED_DATE:
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
                var (count, result) = await _tutorRequestRepository.GetAllWithIncludeAsync(filter,
                               "Parent,ChildInformation", pageSize: 5, pageNumber: pageNumber, orderByQuery, isDesc);
                list = result;
                totalCount = count;

                Pagination pagination = new() { PageNumber = pageNumber, PageSize = 5, Total = totalCount };
                _response.Result = _mapper.Map<List<TutorRequestDTO>>(list);
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

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<APIResponse>> CreateAsync(TutorRequestCreateDTO tutorRequestCreateDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (tutorRequestCreateDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.TUTOR_REQUEST) };
                    return BadRequest(_response);
                }
                TutorRequest model = _mapper.Map<TutorRequest>(tutorRequestCreateDTO);
                model.ParentId = userId;
                model.CreatedDate = DateTime.Now;
                var createdObject = await _tutorRequestRepository.CreateAsync(model);
                var tutor = await _userRepository.GetAsync(x => x.Id == model.TutorId);
                var parent = await _userRepository.GetAsync(x => x.Id == model.ParentId);
                // Send mail for parent

                var subjectForParent = "Xác nhận Yêu cầu Dạy học";
                var parentTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ParentRequestConfirmationTemplate.cshtml");
                var parentTemplateContent = await System.IO.File.ReadAllTextAsync(parentTemplatePath);
                var parentHtmlMessage = parentTemplateContent
                    .Replace("@Model.ParentFullName", parent.FullName)
                    .Replace("@Model.TutorFullName", tutor.FullName)
                    .Replace("@Model.TutorEmail", tutor.Email)
                    .Replace("@Model.TutorPhoneNumber", tutor.PhoneNumber)
                    .Replace("@Model.RequestDescription", model.Description);
                _messageBus.SendMessage(new EmailLogger()
                {
                    Email = parent.Email,
                    Subject = subjectForParent,
                    Message = parentHtmlMessage
                }, queueName);


                // Send mail for tutor

                var subjectForTutor = "Thông báo Yêu cầu Dạy học Mới";
                var tutorTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "TutorRequestNotificationTemplate.cshtml");
                var tutorTemplateContent = await System.IO.File.ReadAllTextAsync(tutorTemplatePath);
                var tutorHtmlMessage = tutorTemplateContent
                    .Replace("@Model.TutorFullName", tutor.FullName)
                    .Replace("@Model.ParentFullName", parent.FullName)
                    .Replace("@Model.RequestDescription", model.Description);
                _messageBus.SendMessage(
                    new EmailLogger() { Email = tutor.Email, Subject = subjectForTutor, Message = tutorHtmlMessage }, queueName);

                _response.Result = _mapper.Map<TutorRequestDTO>(createdObject);
                _response.StatusCode = HttpStatusCode.Created;
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
        //[Authorize(Policy = "UpdateTutorPolicy")]
        public async Task<IActionResult> ApproveOrRejectWorkExperienceRequest(ChangeStatusTutorRequestDTO changeStatusDTO)
        {
            try
            {
                var userId = _userRepository.GetAsync(x => x.Email == SD.ADMIN_EMAIL_DEFAULT).GetAwaiter().GetResult().Id;
                //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                //if (string.IsNullOrEmpty(userId))
                //{
                //    _response.StatusCode = HttpStatusCode.BadRequest;
                //    _response.IsSuccess = false;
                //    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                //    return BadRequest(_response);
                //}

                TutorRequest model = await _tutorRequestRepository.GetAsync(x => x.Id == changeStatusDTO.Id, false, "Parent,Tutor", null);
                if (changeStatusDTO.StatusChange == (int)Status.APPROVE)
                {
                    model.RequestStatus = Status.APPROVE;
                    model.UpdatedDate = DateTime.Now;
                    model.RejectType = RejectType.Approved;
                    await _tutorRequestRepository.UpdateAsync(model);
                    var tutor = await _userRepository.GetAsync(x => x.Id == model.TutorId);
                    // Send mail
                    var subject = "Yêu cầu dạy học của bạn đến gia sư {tutor.FullName} đã được chấp nhận";
                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ChangeStatusTemplate.cshtml");
                    var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);
                    var htmlMessage = templateContent
                        .Replace("@Model.FullName", model.Parent.FullName)
                        .Replace("@Model.IssueName", $"Yêu cầu dạy học của bạn đến gia sư {tutor.FullName}")
                        .Replace("@Model.IsApproved", true.ToString())
                        .Replace("@Model.IsApprovedString", "Chấp nhận")
                        ;
                    _messageBus.SendMessage(new EmailLogger()
                    {
                        Email = model.Parent.Email,
                        Subject = subject,
                        Message = htmlMessage
                    }, queueName);

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
                    var tutor = await _userRepository.GetAsync(x => x.Id == model.TutorId);
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
                            reason = changeStatusDTO.RejectionReason;
                            break;
                    }

                    var subject = $"Yêu cầu dạy học của bạn đến gia sư {tutor.FullName} đã bị từ chối";
                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ChangeStatusTemplate.cshtml");
                    var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);
                    var htmlMessage = templateContent
                        .Replace("@Model.FullName", model.Parent.FullName)
                        .Replace("@Model.IssueName", $"Yêu cầu dạy học của bạn đến gia sư {tutor.FullName}")
                        .Replace("@Model.IsApproved", false.ToString())
                        .Replace("@Model.IsApprovedString", "Từ chối")
                        .Replace("@Model.RejectionReason", reason);
                    _messageBus.SendMessage(new EmailLogger()
                    {
                        Email = model.Parent.Email,
                        Subject = subject,
                        Message = htmlMessage
                    }, queueName);

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
