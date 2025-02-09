﻿using AutismEduConnectSystem.DTOs;
using AutismEduConnectSystem.DTOs.CreateDTOs;
using AutismEduConnectSystem.DTOs.UpdateDTOs;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Resources;
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

namespace AutismEduConnectSystem.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersionNeutral]
    public class ReportController : ControllerBase
    {
        private readonly IReportRepository _reportRepository;
        private readonly IReportMediaRepository _reportMediaRepository;
        private readonly IBlobStorageRepository _blobStorageRepository;
        private readonly IStudentProfileRepository _studentProfileRepository;
        private readonly IChildInformationRepository _childInformationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        private readonly ILogger<ReportController> _logger;
        private readonly IResourceService _resourceService;
        private string queueName = string.Empty;
        private readonly IEmailSender _messageBus;

        public ReportController(IReportRepository reportRepository,
            IReportMediaRepository reportMediaRepository,
            IMapper mapper, IResourceService resourceService,
            ILogger<ReportController> logger,
            IStudentProfileRepository studentProfileRepository,
            IChildInformationRepository childInformationRepository,
            IBlobStorageRepository blobStorageRepository,
            IUserRepository userRepository, IReviewRepository reviewRepository, 
            IConfiguration configuration,
            IEmailSender messageBus)
        {
            _userRepository = userRepository;
            _blobStorageRepository = blobStorageRepository;
            _childInformationRepository = childInformationRepository;
            _studentProfileRepository = studentProfileRepository;
            _reportMediaRepository = reportMediaRepository;
            _response = new APIResponse();
            _mapper = mapper;
            _reportRepository = reportRepository;
            _resourceService = resourceService;
            _logger = logger;
            _reviewRepository = reviewRepository;
            queueName = configuration["RabbitMQSettings:QueueName"];
            _messageBus = messageBus;
        }


        [HttpPost("AppealBan")]
        public async Task<ActionResult<APIResponse>> CreateReportAppealBanAsync(ReportAppealBanCreateDTO createDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.REPORT) };
                    return BadRequest(_response);
                }
                var existReport = await _reportRepository.GetAllNotPagingAsync(x => x.ReportType == SD.ReportType.UNLOCKREQUEST && x.Email == createDTO.Email && x.Status == SD.Status.PENDING);
                if (existReport.list != null && existReport.list.Any())
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.REPORT) };
                    return BadRequest(_response);
                }
                var user = await _userRepository.GetAsync(x => x.Email == createDTO.Email);
                if (user != null && user.LockoutEnd <= DateTime.Now)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.REPORT) };
                    return BadRequest(_response);
                }
                var newModel = _mapper.Map<Report>(createDTO);

                if (user != null) newModel.ReporterId = user.Id;
                newModel.ReportType = SD.ReportType.UNLOCKREQUEST;
                newModel.Title = SD.REPORT_UNLOCKREQUEST_TITLE;

                var reportModel = await _reportRepository.CreateAsync(newModel);
                var result = await _reportRepository.GetAsync(x => x.Id == reportModel.Id, false, "Reporter", null);

                _response.StatusCode = HttpStatusCode.Created;
                _response.Result = _mapper.Map<ReportAppealBanDTO>(result);
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPost("review")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> CreateReportReviewAsync(ReportReviewCreateDTO createDTO)
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
                if (!ModelState.IsValid)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.REPORT) };
                    return BadRequest(_response);
                }
                var existReport = await _reportRepository.GetAllNotPagingAsync(x => x.ReportType == SD.ReportType.REVIEW && x.ReporterId == userId && x.Status == SD.Status.PENDING);
                if (existReport.list != null && existReport.list.Any())
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.REPORT) };
                    return BadRequest(_response);
                }
                var newModel = _mapper.Map<Report>(createDTO);

                newModel.ReporterId = userId;
                newModel.ReportType = SD.ReportType.REVIEW;
                newModel.Title = SD.REPORT_REVIEW_TITLE;
                
                var reportModel = await _reportRepository.CreateAsync(newModel);
                var result = await _reportRepository.GetAsync(x => x.Id == reportModel.Id, false, "Review,Reporter", null);
                _response.StatusCode = HttpStatusCode.Created;
                _response.Result = _mapper.Map<ReportReviewDTO>(result);
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPost("tutor")]
        [Authorize(Roles = SD.PARENT_ROLE)]
        public async Task<ActionResult<APIResponse>> CreateReportTutorAsync([FromForm] ReportTutorCreateDTO createDTO)
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
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.REPORT) };
                    return BadRequest(_response);
                }
                var existReport = await _reportRepository.GetAllNotPagingAsync(x => x.ReportType == SD.ReportType.TUTOR && x.ReporterId == userId && x.Status == SD.Status.PENDING);
                if (existReport.list != null && existReport.list.Any())
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.REPORT) };
                    return BadRequest(_response);
                }
                var child = await _childInformationRepository.GetAllNotPagingAsync(x => x.ParentId == userId);

                if (child.list != null && !child.list.Any())
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.REPORT) };
                    return BadRequest(_response);
                }
                var studentProfiles = await _studentProfileRepository.GetAsync(x => x.Id == createDTO.StudentProfileId);
                if (child.list != null)
                {
                    if (studentProfiles != null && studentProfiles.TutorId == createDTO.TutorId && child.list.Any(u => u.Id == studentProfiles.ChildId))
                    {
                        var newModel = _mapper.Map<Report>(createDTO);

                        newModel.ReporterId = userId;
                        newModel.ReportType = SD.ReportType.TUTOR;

                        var reportModel = await _reportRepository.CreateAsync(newModel);

                        foreach (var media in createDTO.ReportMedias)
                        {
                            using var stream = media.OpenReadStream();
                            var url = await _blobStorageRepository.Upload(stream, string.Concat(Guid.NewGuid().ToString(), Path.GetExtension(media.FileName)));
                            var objMedia = new ReportMedia() { ReportId = reportModel.Id, UrlMedia = url, CreatedDate = DateTime.Now };
                            await _reportMediaRepository.CreateAsync(objMedia);
                        }

                        var result = await _reportRepository.GetAsync(x => x.Id == reportModel.Id, false, "Tutor,Reporter,ReportMedias", null);
                        _response.StatusCode = HttpStatusCode.Created;
                        _response.Result = _mapper.Map<ReportTutorDTO>(result);
                        _response.IsSuccess = true;
                        return Ok(_response);
                    }
                }
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.REPORT) };
                return BadRequest(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpGet]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? search, string? status = SD.STATUS_ALL, DateTime? startDate = null, DateTime? endDate = null, string? type = SD.TYPE_ALL, int? reportTutorType = 0, string? orderBy = SD.CREATED_DATE, string? sort = SD.ORDER_DESC, int pageNumber = 1)
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
                Expression<Func<Report, bool>> filter = u => true;
                Expression<Func<Report, object>> orderByQuery = u => true;
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

                if (userRoles.Contains(SD.PARENT_ROLE) || userRoles.Contains(SD.TUTOR_ROLE))
                {

                    filter = u => !string.IsNullOrEmpty(u.ReporterId) && u.ReporterId == userId;
                }

                

                if (startDate != null)
                    filter.AndAlso(x => x.CreatedDate.Date >= startDate.Value.Date);

                if (endDate != null)
                    filter.AndAlso(x => x.CreatedDate.Date <= endDate.Value.Date);

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
                            filter = filter.AndAlso(x => x.Status == Status.APPROVE);
                            break;
                        case "reject":
                            filter = filter.AndAlso(x => x.Status == Status.REJECT);
                            break;
                        case "pending":
                            filter = filter.AndAlso(x => x.Status == Status.PENDING);
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(type) && type != SD.TYPE_ALL)
                {
                    switch (type.ToLower())
                    {
                        case "review":
                            filter = filter.AndAlso(x => x.ReportType == ReportType.REVIEW);
                            break;
                        case "tutor":
                            filter = filter.AndAlso(x => x.ReportType == ReportType.TUTOR);
                            if (reportTutorType != 0) filter = filter.AndAlso(x => (int)x.ReportTutorType == reportTutorType);
                            if (search != null && !string.IsNullOrEmpty(search))
                                filter = filter.AndAlso(x => x.Tutor != null && x.Tutor.User != null && (x.Tutor.User.Email.ToLower().Contains(search.ToLower()) || x.Tutor.User.FullName.Contains(search.ToLower())));
                            break;
                        case "account":
                            filter = filter.AndAlso(x => x.ReportType == ReportType.UNLOCKREQUEST);
                            break;
                    }
                }

                var (count, result) = await _reportRepository.GetAllWithIncludeAsync(
                    filter,
                    "Tutor,Review,Reporter",
                    pageSize: 10,
                    pageNumber: pageNumber,
                    orderByQuery,
                    isDesc
                );

                foreach (var item in result)
                {
                    if(item.Tutor != null)
                    {
                        item.Tutor.User = await _userRepository.GetAsync(x => x.Id.Equals(item.TutorId));
                    }else if(item.Review != null)
                    {
                        item.Review.Parent = await _userRepository.GetAsync(x => x.Id.Equals(item.Review.ParentId));
                    }
                }

                // Setup pagination and response
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = 10, Total = count };
                _response.Result = _mapper.Map<List<ReportDTO>>(result);
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

        [HttpPut("changeStatus/{id}")]
        [Authorize(Roles = $"{SD.STAFF_ROLE},{SD.MANAGER_ROLE}")]
        public async Task<ActionResult<APIResponse>> UpdateStatusRequest(ReportUpdateDTO updateDTO)
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
                if (userRoles == null || (!userRoles.Contains(SD.MANAGER_ROLE) && !userRoles.Contains(SD.STAFF_ROLE)))
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
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.REPORT) };
                    return BadRequest(_response);
                }

                Report model = await _reportRepository.GetAsync(x => x.Id == updateDTO.Id, true, "Reporter", null);
                List<Report> reports = new();
                if (model == null || model.Status != Status.PENDING)
                {

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.REPORT) };
                    return BadRequest(_response);
                }

                if (updateDTO.StatusChange == (int)Status.APPROVE)
                {
                    model.Status = Status.APPROVE;
                    model.UpdatedDate = DateTime.Now;
                    model.Comments = updateDTO.Comment;
                    model.HandlerId = userId;

                    // Send mail
                    var user = await _userRepository.GetAsync(x => x.Id.Equals(model.ReporterId));
                    var tutor = await _userRepository.GetAsync(x => x.Id.Equals(model.TutorId));

                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ChangeStatusTemplate.cshtml");
                    if (System.IO.File.Exists(templatePath) && user != null && tutor != null)
                    {
                        var subject = "Đơn tố cáo của bạn đã của bạn đã được chấp nhận!";
                        var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);

                        var rejectionReasonHtml = string.Empty;
                        var htmlMessage = templateContent
                        .Replace("@Model.FullName", user.FullName)
                        .Replace("@Model.IssueName", $"Đơn tố cáo gia sư {tutor.FullName}")
                        .Replace("@Model.IsApprovedString", "Chấp nhận")
                        .Replace("@Model.RejectionReason", rejectionReasonHtml)
                        .Replace("@Model.Mail", SD.MAIL)
                        .Replace("@Model.Phone", SD.PHONE_NUMBER)
                        .Replace("@Model.WebsiteURL", SD.URL_FE);

                        await _messageBus.SendEmailAsync(user.Email, subject, htmlMessage);
                    }

                    await _reportRepository.UpdateAsync(model);
                    if (!string.IsNullOrEmpty(model.TutorId))
                    {
                        var (countReportTutor, listReportTutor) = await _reportRepository.GetAllNotPagingAsync(x => x.TutorId == model.TutorId && x.Id != model.Id && x.Status == SD.Status.PENDING, null, null, x => x.CreatedDate, true);
                        reports = listReportTutor;
                    }
                    else if (model.ReviewId != null && model.ReviewId > 0)
                    {
                        var (countReportReview, listReportReview) = await _reportRepository.GetAllNotPagingAsync(x => x.ReviewId == model.ReviewId && x.Id != model.Id && x.Status == SD.Status.PENDING, null, null, x => x.CreatedDate, true);
                        reports = listReportReview;
                        var review = await _reviewRepository.GetAsync(x => x.Id == model.ReviewId, true, null, null);
                        review.IsHide = true;
                        await _reviewRepository.UpdateAsync(review);
                    }
                    foreach (var item in reports)
                    {
                        item.Status = Status.APPROVE;
                        item.UpdatedDate = DateTime.Now;
                        item.Comments = updateDTO.Comment;
                        item.HandlerId = userId;
                        await _reportRepository.UpdateAsync(item);

                        // Send mail
                        var otherUser = await _userRepository.GetAsync(x => x.Id.Equals(item.ReporterId));
                        var otherTutor = await _userRepository.GetAsync(x => x.Id.Equals(item.TutorId));

                        var otherTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ChangeStatusTemplate.cshtml");
                        if (System.IO.File.Exists(otherTemplatePath) && otherUser != null && otherTutor != null)
                        {
                            var subject = "Đơn tố cáo của bạn đã của bạn đã được chấp nhận!";
                            var templateContent = await System.IO.File.ReadAllTextAsync(otherTemplatePath);

                            var rejectionReasonHtml = string.Empty;
                            var htmlMessage = templateContent
                            .Replace("@Model.FullName", otherUser.FullName)
                            .Replace("@Model.IssueName", $"Đơn tố cáo gia sư {otherTutor.FullName}")
                            .Replace("@Model.IsApprovedString", "Chấp nhận")
                            .Replace("@Model.RejectionReason", rejectionReasonHtml)
                            .Replace("@Model.Mail", SD.MAIL)
                            .Replace("@Model.Phone", SD.PHONE_NUMBER)
                            .Replace("@Model.WebsiteURL", SD.URL_FE);

                            await _messageBus.SendEmailAsync(otherUser.Email, subject, htmlMessage);
                        }
                    }
                    _response.Result = _mapper.Map<ReportDTO>(model);
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    return Ok(_response);
                }
                else if (updateDTO.StatusChange == (int)Status.REJECT)
                {
                    // Handle for reject
                    model.Status = Status.REJECT;
                    model.UpdatedDate = DateTime.Now;
                    model.Comments = updateDTO.Comment;
                    model.HandlerId = userId;

                    // Send mail
                    var user = await _userRepository.GetAsync(x => x.Id.Equals(model.ReporterId));
                    var tutor = await _userRepository.GetAsync(x => x.Id.Equals(model.TutorId));

                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ChangeStatusTemplate.cshtml");
                    if (System.IO.File.Exists(templatePath) && user != null && tutor != null)
                    {
                        var subject = "Đơn tố cáo của bạn đã bị từ chối!";
                        var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);

                        var rejectionReasonHtml = $"<p><strong>Lý do từ chối:</strong> {updateDTO.Comment}</p>";
                        var htmlMessage = templateContent
                        .Replace("@Model.FullName", user.FullName)
                        .Replace("@Model.IssueName", $"Đơn tố cáo gia sư {tutor.FullName}")
                        .Replace("@Model.IsApprovedString", "Từ chôi")
                        .Replace("@Model.RejectionReason", rejectionReasonHtml)
                        .Replace("@Model.Mail", SD.MAIL)
                        .Replace("@Model.Phone", SD.PHONE_NUMBER)
                        .Replace("@Model.WebsiteURL", SD.URL_FE);

                        await _messageBus.SendEmailAsync(user.Email, subject, htmlMessage);
                    }

                    await _reportRepository.UpdateAsync(model);
                    if (!string.IsNullOrEmpty(model.TutorId))
                    {
                        var (countReportTutor, listReportTutor) = await _reportRepository.GetAllNotPagingAsync(x => x.TutorId == model.TutorId && x.Id != model.Id && x.Status == SD.Status.PENDING, null, null, x => x.CreatedDate, true);
                        reports = listReportTutor;
                    }
                    else if (model.ReviewId != null && model.ReviewId > 0)
                    {
                        var (countReportReview, listReportReview) = await _reportRepository.GetAllNotPagingAsync(x => x.ReviewId == model.ReviewId && x.Id != model.Id && x.Status == SD.Status.PENDING, null, null, x => x.CreatedDate, true);
                        reports = listReportReview;
                    }
                    foreach (var item in reports)
                    {
                        item.Status = Status.REJECT;
                        item.UpdatedDate = DateTime.Now;
                        item.Comments = updateDTO.Comment;
                        item.HandlerId = userId;
                        await _reportRepository.UpdateAsync(item);

                        // Send mail
                        var otherUser = await _userRepository.GetAsync(x => x.Id.Equals(item.ReporterId));
                        var otherTutor = await _userRepository.GetAsync(x => x.Id.Equals(item.TutorId));

                        var otherTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ChangeStatusTemplate.cshtml");
                        if (System.IO.File.Exists(otherTemplatePath) && otherUser != null && otherTutor != null)
                        {                         
                            var subject = "Đơn tố cáo của bạn đã bị từ chối!";
                            var templateContent = await System.IO.File.ReadAllTextAsync(otherTemplatePath);

                            var rejectionReasonHtml = $"<p><strong>Lý do từ chối:</strong> {updateDTO.Comment}</p>";
                            var htmlMessage = templateContent
                            .Replace("@Model.FullName", otherUser.FullName)
                            .Replace("@Model.IssueName", $"Đơn tố cáo gia sư {otherTutor.FullName}")
                            .Replace("@Model.IsApprovedString", "Từ chôi")
                            .Replace("@Model.RejectionReason", rejectionReasonHtml)
                            .Replace("@Model.Mail", SD.MAIL)
                            .Replace("@Model.Phone", SD.PHONE_NUMBER)
                            .Replace("@Model.WebsiteURL", SD.URL_FE);

                            await _messageBus.SendEmailAsync(otherUser.Email, subject, htmlMessage);
                        }
                    }
                    _response.Result = _mapper.Map<ReportDTO>(model);
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


        [HttpGet("{id}")]
        [Authorize(Roles = $"{SD.MANAGER_ROLE},{SD.STAFF_ROLE}")]
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
                if (userRoles == null || (!userRoles.Contains(SD.MANAGER_ROLE) && !userRoles.Contains(SD.STAFF_ROLE)))
                {
                   
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                Report report = await _reportRepository.GetAsync(x => x.Id == id, false, "Handler,Tutor,Review,Reporter,ReportMedias", null);
                List<Report> reports = new();
                if (report == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.REPORT) };
                    return NotFound(_response);
                }
                if (!string.IsNullOrEmpty(report.TutorId))
                {
                    var (countReportTutor, listReportTutor) = await _reportRepository.GetAllNotPagingAsync(x => x.TutorId == report.TutorId && x.Id != report.Id && x.Status == SD.Status.PENDING, "Handler,Reporter,ReportMedias", null, x => x.CreatedDate, true);
                    reports = listReportTutor;
                }
                else if (report.ReviewId != null && report.ReviewId > 0)
                {
                    var (countReportReview, listReportReview) = await _reportRepository.GetAllNotPagingAsync(x => x.ReviewId == report.ReviewId && x.Id != report.Id && x.Status == SD.Status.PENDING, "Handler,Reporter", null, x => x.CreatedDate, true);
                    reports = listReportReview;
                }
                if (report.Tutor != null)
                {
                    report.Tutor.User = await _userRepository.GetAsync(x => x.Id.Equals(report.TutorId));
                }else if(report.Review != null)
                {
                    report.Review.Parent = await _userRepository.GetAsync(x => x.Id == report.Review.ParentId);
                }
                _response.Result = new { Result = _mapper.Map<ReportDTO>(report), ReportsRelated = _mapper.Map<List<ReportDTO>>(reports) };
                _response.StatusCode = HttpStatusCode.OK;
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
