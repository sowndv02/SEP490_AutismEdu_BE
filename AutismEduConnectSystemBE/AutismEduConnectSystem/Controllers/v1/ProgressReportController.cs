﻿using AutismEduConnectSystem.DTOs;
using AutismEduConnectSystem.DTOs.CreateDTOs;
using AutismEduConnectSystem.DTOs.UpdateDTOs;
using AutismEduConnectSystem.Models;
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

namespace AutismEduConnectSystem.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class ProgressReportController : ControllerBase
    {
        protected APIResponse _response;
        private readonly IMapper _mapper;
        private readonly IProgressReportRepository _progressReportRepository;
        private readonly IAssessmentResultRepository _assessmentResultRepository;
        private readonly IInitialAssessmentResultRepository _initialAssessmentResultRepository;
        private readonly IStudentProfileRepository _studentProfileRepository;
        private readonly IChildInformationRepository _childInformationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IResourceService _resourceService;
        private readonly ILogger<ProgressReportController> _logger;
        private readonly INotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationHub> _hubContext;
        public ProgressReportController(IMapper mapper, IConfiguration configuration,
            IProgressReportRepository progressReportRepository, IAssessmentResultRepository assessmentResultRepository,
            IInitialAssessmentResultRepository initialAssessmentResultRepository, IResourceService resourceService, ILogger<ProgressReportController> logger,
            INotificationRepository notificationRepository, IHubContext<NotificationHub> hubContext, IStudentProfileRepository studentProfileRepository,
            IChildInformationRepository childInformationRepository, IUserRepository userRepository)
        {
            _response = new APIResponse();
            _mapper = mapper;
            _progressReportRepository = progressReportRepository;
            _assessmentResultRepository = assessmentResultRepository;
            _initialAssessmentResultRepository = initialAssessmentResultRepository;
            _resourceService = resourceService;
            _logger = logger;
            _notificationRepository = notificationRepository;
            _hubContext = hubContext;
            _studentProfileRepository = studentProfileRepository;
            _childInformationRepository = childInformationRepository;
            _userRepository = userRepository;
        }

        [HttpPost]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> CreateAsync(ProgressReportCreateDTO createDTO)
        {
            try
            {
                var tutorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(tutorId))
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
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.PROGRESS_REPORT) };
                    return BadRequest(_response);
                }

                var model = _mapper.Map<ProgressReport>(createDTO);
                model.TutorId = tutorId;
                model.CreatedDate = DateTime.Now;

                var progressReport = await _progressReportRepository.CreateAsync(model);
                List<AssessmentResult> assessmentResults = new List<AssessmentResult>();
                if (progressReport.AssessmentResults != null) 
                {
                    foreach (var assessmentResult in progressReport.AssessmentResults)
                    {
                        assessmentResults.Add(await _assessmentResultRepository.GetAsync(x => x.Id == assessmentResult.Id, true, "Question,Option"));
                    }
                }
                progressReport.AssessmentResults = assessmentResults;

                // Notification
                await SendNotificationWhenCreateProgressReport(createDTO.StudentProfileId, progressReport.Id);

                _response.Result = _mapper.Map<ProgressReportDTO>(progressReport);
                _response.StatusCode = HttpStatusCode.Created;
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
        private async Task SendNotificationWhenCreateProgressReport(int studentProfileId, int progressReportId)
        {
            var studentProfile = await _studentProfileRepository.GetAsync(x => x.Id == studentProfileId);
            if(studentProfile != null)
            {
                var child = await _childInformationRepository.GetAsync(x => x.Id == studentProfile.ChildId, false, null, null);
                if (child != null)
                {
                    var tutor = await _userRepository.GetAsync(x => x.Id == studentProfile.TutorId);
                    if (tutor != null)
                    {
                        var connectionId = NotificationHub.GetConnectionIdByUserId(child.ParentId);
                        var notfication = new Notification()
                        {
                            ReceiverId = child.ParentId,
                            Message = _resourceService.GetString(SD.CREATE_PROGRESS_REPORT_PARENT_NOTIFICATION, tutor.FullName),
                            UrlDetail = string.Concat(SD.URL_FE, SD.URL_FE_PARENT_STUDENT_PROFILE_LIST, studentProfileId),
                            IsRead = false,
                            CreatedDate = DateTime.Now
                        };
                        var notificationResult = await _notificationRepository.CreateAsync(notfication);
                        if (!string.IsNullOrEmpty(connectionId))
                        {
                            await _hubContext.Clients.Client(connectionId).SendAsync($"Notifications-{tutor.Id}", _mapper.Map<NotificationDTO>(notificationResult));
                        }
                    }
                }
            }
        }

        [HttpGet]
        [Authorize(Roles = $"{SD.TUTOR_ROLE},{SD.PARENT_ROLE}")]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] int studentProfileId, DateTime? startDate = null, DateTime? endDate = null, string? orderBy = SD.CREATED_DATE, string? sort = SD.ORDER_DESC, int pageNumber = 1, int pageSize = 10,bool getInitialResult = false)
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
                if (userRoles == null || (!userRoles.Contains(SD.TUTOR_ROLE) && !userRoles.Contains(SD.PARENT_ROLE)))
                {
                   
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                if(studentProfileId <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                if (startDate != null && endDate != null && startDate > endDate)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.SEARCH_DATE) };
                    return BadRequest(_response);
                }


                Expression<Func<ProgressReport, bool>> filter = u => true;
                Expression<Func<ProgressReport, object>> orderByQuery = u => true;
                bool isDesc = sort != null && sort == SD.ORDER_DESC;

                filter = u => u.StudentProfileId == studentProfileId;

                if (orderBy != null)
                {
                    switch (orderBy)
                    {
                        case SD.CREATED_DATE:
                            orderByQuery = x => x.CreatedDate;
                            break;
                        case SD.DATE_FROM:
                            orderByQuery = x => x.From;
                            break;
                        case SD.DATE_TO:
                            orderByQuery = x => x.To;
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
                    filter = filter.AndAlso(u => u.From.Date >= startDate.Value.Date);
                }
                if (endDate != null)
                {
                    filter = filter.AndAlso(u => u.To.Date <= endDate.Value.Date);
                }


                var (count, result) = await _progressReportRepository.GetAllAsync(filter,
                                "StudentProfile,AssessmentResults", pageSize: pageSize, pageNumber: pageNumber, orderByQuery, isDesc);

                if (result != null && result.Any())
                {
                    foreach (var item in result)
                    {
                        List<AssessmentResult> assessmentResults = new List<AssessmentResult>();
                        if(item.AssessmentResults != null && item.AssessmentResults.Any())
                        {
                            foreach (var assessmentResult in item.AssessmentResults)
                            {
                                assessmentResults.Add(await _assessmentResultRepository.GetAsync(x => x.Id == assessmentResult.Id, true, "Question,Option"));
                            }
                            item.AssessmentResults = assessmentResults;
                        }
                    }
                }

                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = count };

                if (getInitialResult)
                {
                    ProgressReportGraphDTO graph = new ProgressReportGraphDTO();
                    graph.ProgressReports = _mapper.Map<List<ProgressReportDTO>>(result);

                    var initialAssessmentResult = await _initialAssessmentResultRepository.GetAllAsync(x => x.StudentProfileId == studentProfileId, "Question,Option");
                    graph.InitialAssessmentResultDTO = _mapper.Map<List<InitialAssessmentResultDTO>>(initialAssessmentResult.list);

                    _response.Result = graph;
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.Pagination = pagination;
                    return Ok(_response);
                }

                _response.Result = _mapper.Map<List<ProgressReportDTO>>(result);
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

        [HttpGet("{Id}")]
        [Authorize(Roles = $"{SD.TUTOR_ROLE},{SD.PARENT_ROLE}")]
        public async Task<ActionResult<APIResponse>> GetByIdÁync(int Id)
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
                if (userRoles == null || (!userRoles.Contains(SD.TUTOR_ROLE) && !userRoles.Contains(SD.PARENT_ROLE)))
                {
                   
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                if (Id <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                var progressReport = await _progressReportRepository.GetAsync(x => x.Id == Id, true, "AssessmentResults");

                if (progressReport == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.PROGRESS_REPORT) };
                    return NotFound(_response);
                }

                List<AssessmentResult> assessmentResults = new List<AssessmentResult>();
                if(progressReport.AssessmentResults != null && progressReport.AssessmentResults.Any())
                {
                    foreach (var assessmentResult in progressReport.AssessmentResults)
                    {
                        assessmentResults.Add(await _assessmentResultRepository.GetAsync(x => x.Id == assessmentResult.Id, true, "Question,Option"));
                    }
                    progressReport.AssessmentResults = assessmentResults;
                }

                _response.Result = _mapper.Map<ProgressReportDTO>(progressReport);
                _response.StatusCode = HttpStatusCode.OK;
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

        [HttpPut]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> UpdateAsync(ProgressReportUpdateDTO updateDTO)
        {
            try
            {
                var tutorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(tutorId))
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
                if (updateDTO.Id <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                if (updateDTO == null || !ModelState.IsValid)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.PROGRESS_REPORT) };
                    return BadRequest(_response);
                }

                var model = await _progressReportRepository.GetAsync(x => x.Id == updateDTO.Id && x.TutorId == tutorId);

                if (model == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.PROGRESS_REPORT) };
                    return NotFound(_response);
                }

                if (model.CreatedDate.AddHours(48) <= DateTime.Now)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.PROGRESS_REPORT_MODIFICATION_EXPIRED) };
                    return BadRequest(_response);
                }

                model.Achieved = updateDTO.Achieved;
                model.Failed = updateDTO.Failed;
                model.NoteFromTutor = updateDTO.NoteFromTutor;

                await _progressReportRepository.UpdateAsync(model);

                List<AssessmentResult> updatedAssessmentResults = new List<AssessmentResult>();
                if(updateDTO.AssessmentResults != null && updateDTO.AssessmentResults.Any())
                {
                    foreach (var updatedAssessmentResult in updateDTO.AssessmentResults)
                    {
                        var assessmentResult = await _assessmentResultRepository.GetAsync(x => x.Id == updatedAssessmentResult.Id);
                        if (assessmentResult != null) 
                        {
                            assessmentResult.QuestionId = updatedAssessmentResult.QuestionId;
                            assessmentResult.OptionId = updatedAssessmentResult.OptionId;
                            await _assessmentResultRepository.UpdateAsync(assessmentResult);
                        }
                        assessmentResult = await _assessmentResultRepository.GetAsync(x => x.Id == updatedAssessmentResult.Id, true, "Question,Option");
                        if (assessmentResult != null) 
                        {
                            updatedAssessmentResults.Add(assessmentResult);
                        }
                    }
                }
                model.AssessmentResults = updatedAssessmentResults;

                // Notification
                await SendNotificationWhenUpdateProgressReport(model);

                _response.Result = _mapper.Map<ProgressReportDTO>(model);
                _response.StatusCode = HttpStatusCode.Created;
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

        private async Task SendNotificationWhenUpdateProgressReport(ProgressReport model)
        {
            var studentProfile = await _studentProfileRepository.GetAsync(x => x.Id == model.StudentProfileId);
            if (studentProfile != null) 
            {
                var child = await _childInformationRepository.GetAsync(x => x.Id == studentProfile.ChildId, false, null, null);
                if (child != null) 
                {
                    var tutor = await _userRepository.GetAsync(x => x.Id == studentProfile.TutorId);
                    if(tutor != null)
                    {
                        var connectionId = NotificationHub.GetConnectionIdByUserId(child.ParentId);
                        var notfication = new Notification()
                        {
                            ReceiverId = child.ParentId,
                            Message = _resourceService.GetString(SD.UPDATE_PROGRESS_REPORT_PARENT_NOTIFICATION, tutor.FullName, model.From.ToString("dd/MM/yyyy"), model.To.ToString("dd/MM/yyyy")),
                            UrlDetail = string.Concat(SD.URL_FE, SD.URL_FE_PARENT_STUDENT_PROFILE_LIST),
                            IsRead = false,
                            CreatedDate = DateTime.Now
                        };
                        var notificationResult = await _notificationRepository.CreateAsync(notfication);
                        if (!string.IsNullOrEmpty(connectionId))
                        {
                            await _hubContext.Clients.Client(connectionId).SendAsync($"Notifications-{child.ParentId}", _mapper.Map<NotificationDTO>(notificationResult));
                        }
                    }
                }
            }
        }
    }
}
