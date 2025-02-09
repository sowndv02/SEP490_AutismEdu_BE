﻿using AutismEduConnectSystem.DTOs;
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

namespace AutismEduConnectSystem.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class ScheduleController : ControllerBase
    {
        private readonly IScheduleRepository _scheduleRepository;
        private readonly IStudentProfileRepository _studentProfileRepository;
        private readonly IResourceService _resourceService;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        private readonly IChildInformationRepository _childInfoRepository;
        private readonly ISyllabusRepository _syllabusRepository;
        private readonly ILogger<ScheduleController> _logger;
        protected int pageSize = 0;
        private readonly INotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IEmailSender _messageBus;
        private string queueName = string.Empty;
        private readonly ITutorRepository _tutorRepository;

        public ScheduleController(IScheduleRepository scheduleRepository, IMapper mapper
            , IStudentProfileRepository studentProfileRepository, IResourceService resourceService,
            IChildInformationRepository childInfoRepository, ISyllabusRepository syllabusRepository, 
            ILogger<ScheduleController> logger, IConfiguration configuration, IEmailSender messageBus,
            INotificationRepository notificationRepository, IHubContext<NotificationHub> hubContext, ITutorRepository tutorRepository)
        {
            _resourceService = resourceService;
            _scheduleRepository = scheduleRepository;
            _response = new APIResponse();
            _mapper = mapper;
            _studentProfileRepository = studentProfileRepository;
            _childInfoRepository = childInfoRepository;
            _syllabusRepository = syllabusRepository;
            _logger = logger;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            queueName = configuration["RabbitMQSettings:QueueName"];
            _messageBus = messageBus;
            _notificationRepository = notificationRepository;
            _hubContext = hubContext;
            _tutorRepository = tutorRepository;
        }

        [HttpGet("{id}")]
        [Authorize]
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

                if (id <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                var model = await _scheduleRepository.GetAsync(x => x.Id == id, false, "Exercise,ExerciseType");
                if (model == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.SCHEDULE) };
                    return NotFound(_response);
                }
                model.StudentProfile = await _studentProfileRepository.GetAsync(x => x.Id == model.StudentProfileId, true, "Child");
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = _mapper.Map<ScheduleDTO>(model);
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



        [HttpPut("AssignExercises/{id}")]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> AssignExercises(int id, [FromBody] AssignExerciseScheduleDTO updateDTO)
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
                if (id <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                if (updateDTO == null || updateDTO.Id != id || !ModelState.IsValid)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.SCHEDULE) };
                    return BadRequest(_response);
                }
                var model = await _scheduleRepository.GetAsync(x => x.Id == id, false, null);
                if (model == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.SCHEDULE) };
                    return NotFound(_response);
                }
                model.ExerciseId = updateDTO.ExerciseId;
                model.ExerciseTypeId = updateDTO.ExerciseTypeId;
                model.SyllabusId = updateDTO.SyllabusId;
                model.UpdatedDate = DateTime.Now;
                await _scheduleRepository.UpdateAsync(model);
                var result = await _scheduleRepository.GetAsync(x => x.Id == id, false, "ExerciseType,Exercise");
                result.StudentProfile = await _studentProfileRepository.GetAsync(x => x.Id == model.StudentProfileId, true, "Child");
                result.Tutor = await _tutorRepository.GetAsync(x => x.TutorId.Equals(userId), false, "User");

                // Notification
                var child = await _childInfoRepository.GetAsync(x => x.Id == result.StudentProfile.ChildId, true, "Parent");

                if (child != null && child.Parent != null)
                {
                    var connectionId = NotificationHub.GetConnectionIdByUserId(child.ParentId);
                    var notfication = new Notification()
                    {
                        ReceiverId = child.ParentId,
                        Message = _resourceService.GetString(SD.ASSIGNED_EXERCISE_NOTIFICATION, result.ScheduleDate.ToString("dd/MM/yyyy"), result.Start.ToString(@"hh\:mm"), result.End.ToString(@"hh\:mm")),
                        UrlDetail = string.Concat(SD.URL_FE, SD.URL_FE_PARENT_STUDENT_PROFILE_LIST, result.StudentProfileId),
                        IsRead = false,
                        CreatedDate = DateTime.Now
                    };
                    var notificationResult = await _notificationRepository.CreateAsync(notfication);
                    if (!string.IsNullOrEmpty(connectionId))
                    {
                        await _hubContext.Clients.Client(connectionId).SendAsync($"Notifications-{child.ParentId}", _mapper.Map<NotificationDTO>(notificationResult));
                    }
                }


                var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "AssignedExerciseTemplate.cshtml");
                if (System.IO.File.Exists(templatePath) && child.Parent != null && result.Tutor != null && result.Tutor.User != null)
                {
                    var subject = "Thông Báo Lịch Học Đã Được Gán Bài Tập";
                    var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);
                    var htmlMessage = templateContent
                        .Replace("@Model.ParentName", child.Parent.FullName)
                        .Replace("@Model.TutorName", result.Tutor.User.FullName)
                        .Replace("@Model.StudentName", child.Name)
                        .Replace("@Model.ScheduleDate", result.ScheduleDate.ToString("dd/MM/yyyy"))
                        .Replace("@Model.Start", result.Start.ToString(@"hh\:mm"))
                        .Replace("@Model.End", result.End.ToString(@"hh\:mm"))
                        .Replace("@Model.Url", string.Concat(SD.URL_FE, SD.URL_FE_PARENT_STUDENT_PROFILE_LIST, result.StudentProfileId))
                        .Replace("@Model.Mail", SD.MAIL)
                        .Replace("@Model.Phone", SD.PHONE_NUMBER)
                        .Replace("@Model.WebsiteURL", SD.URL_FE);

                    await _messageBus.SendEmailAsync(child.Parent.Email, subject, htmlMessage);
                    
                }

                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                _response.Result = _mapper.Map<ScheduleDTO>(result);
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

        [HttpGet("NotPassed/{studentProfileId}")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllNotPassedExerciseAsync(int studentProfileId)
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
                if (studentProfileId <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }

                var (countFailed, resultFailed) = await _scheduleRepository.GetAllNotPagingAsync(u =>
                                                u.TutorId == userId && u.StudentProfileId == studentProfileId &&
                                                u.AttendanceStatus == SD.AttendanceStatus.ATTENDED &&
                                                u.PassingStatus == SD.PassingStatus.NOT_PASSED,
                                                null, null, x => x.ScheduleDate.Date, true);

                var (countPassed, listPassed) = await _scheduleRepository.GetAllNotPagingAsync(u =>
                                                u.TutorId == userId && u.StudentProfileId == studentProfileId &&
                                                u.AttendanceStatus == SD.AttendanceStatus.ATTENDED &&
                                                u.PassingStatus == SD.PassingStatus.PASSED,
                                                null, null, x => x.ScheduleDate.Date, true);

                var passedExerciseIds = listPassed.Select(p => p.ExerciseId).Distinct();
                var filteredFailedExercises = resultFailed
                    .Where(f => f.ExerciseId == null || !passedExerciseIds.Contains(f.ExerciseId))
                    .ToList().Select(x => x.ExerciseId);

                _response.Result = filteredFailedExercises;
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

        [HttpPut("changeStatus/{id}")]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> UpdateAsync(int id, [FromBody] ScheduleUpdateDTO updateDTO)
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
                if (id <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                if (updateDTO == null || updateDTO.Id != id)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.SCHEDULE) };
                    return BadRequest(_response);
                }
                var model = await _scheduleRepository.GetAsync(x => x.Id == id && x.TutorId == userId, false, "Exercise,ExerciseType");
                if (model == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.SCHEDULE) };
                    return NotFound(_response);
                }
                model.StudentProfile = await _studentProfileRepository.GetAsync(x => x.Id == model.StudentProfileId, true, "Child");
                model.AttendanceStatus = updateDTO.AttendanceStatus;
                model.Note = updateDTO.Note;
                model.PassingStatus = updateDTO.PassingStatus;
                model.UpdatedDate = DateTime.Now;
                var result = await _scheduleRepository.UpdateAsync(model);
                var nextSchedule = await _scheduleRepository.GetAsync(x => x.PassingStatus == SD.PassingStatus.NOT_YET && x.AttendanceStatus == SD.AttendanceStatus.NOT_YET && x.ScheduleDate.Date > result.ScheduleDate.Date && x.ExerciseId == result.ExerciseId && x.ExerciseTypeId == result.ExerciseTypeId, false, null);
                if (nextSchedule != null)
                {
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.DUPPLICATED_ASSIGN_EXERCISE) };
                }

                // Notification
                var child = await _childInfoRepository.GetAsync(x => x.Id == result.StudentProfile.ChildId, true, "Parent");

                if (child != null && child.Parent != null)
                {
                    var connectionId = NotificationHub.GetConnectionIdByUserId(child.ParentId);
                    var notfication = new Notification()
                    {
                        ReceiverId = child.ParentId,
                        Message = _resourceService.GetString(SD.SCHEDULE_UPDATE_NOTIFICATION, result.ScheduleDate.ToString("dd/MM/yyyy"), result.Start.ToString(@"hh\:mm"), result.End.ToString(@"hh\:mm")),
                        UrlDetail = string.Concat(SD.URL_FE, SD.URL_FE_PARENT_STUDENT_PROFILE_LIST, result.StudentProfileId),
                        IsRead = false,
                        CreatedDate = DateTime.Now
                    };
                    var notificationResult = await _notificationRepository.CreateAsync(notfication);
                    if (!string.IsNullOrEmpty(connectionId))
                    {
                        await _hubContext.Clients.Client(connectionId).SendAsync($"Notifications-{child.ParentId}", _mapper.Map<NotificationDTO>(notificationResult));
                    }
                }

                var tutor = await _tutorRepository.GetAsync(x => x.TutorId.Equals(userId), false, "User");

                var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ScheduleUpdatedTemplate.cshtml");
                if (System.IO.File.Exists(templatePath) && child.Parent != null && tutor != null && tutor.User != null)
                {
                    var subject = "Thông Báo Lịch Học Đã Được Đánh Giá";
                    var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);
                    var htmlMessage = templateContent
                        .Replace("@Model.ParentName", child.Parent.FullName)
                        .Replace("@Model.TutorName", tutor.User.FullName)
                        .Replace("@Model.StudentName", child.Name)
                        .Replace("@Model.ScheduleDate", result.ScheduleDate.ToString("dd/MM/yyyy"))
                        .Replace("@Model.Start", result.Start.ToString(@"hh\:mm"))
                        .Replace("@Model.End", result.End.ToString(@"hh\:mm"))
                        .Replace("@Model.Url", string.Concat(SD.URL_FE, SD.URL_FE_PARENT_STUDENT_PROFILE_LIST, result.StudentProfileId))
                        .Replace("@Model.Mail", SD.MAIL)
                        .Replace("@Model.Phone", SD.PHONE_NUMBER)
                        .Replace("@Model.WebsiteURL", SD.URL_FE);

                    await _messageBus.SendEmailAsync(child.Parent.Email, subject, htmlMessage);
                }

                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                _response.Result = _mapper.Map<ScheduleDTO>(result);
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
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] int studentProfileId, DateTime? startDate = null, DateTime? endDate = null, bool getAll = true)
        {
            try
            {
                List<Schedule> list = new();
                Expression<Func<Schedule, bool>> filter = u => true;

                var tutorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(tutorId))
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                if (studentProfileId != 0)
                {
                    filter = u => u.StudentProfileId == studentProfileId && !u.IsHidden;
                }
                else
                {
                    filter = u => u.TutorId.Equals(tutorId) && !u.IsHidden;
                }

                if (startDate != null)
                {
                    filter = filter.AndAlso(u => u.ScheduleDate.Date >= startDate.Value.Date);
                }
                if (endDate != null)
                {
                    filter = filter.AndAlso(u => u.ScheduleDate.Date <= endDate.Value.Date);
                }

                var (count, result) = await _scheduleRepository.GetAllNotPagingAsync(filter,
                                "StudentProfile,Exercise,ExerciseType", null, null, true);

                foreach (Schedule schedule in result)
                {
                    schedule.StudentProfile = await _studentProfileRepository.GetAsync(x => x.Id == schedule.StudentProfileId);
                    schedule.StudentProfile.Child = await _childInfoRepository.GetAsync(x => x.Id == schedule.StudentProfile.ChildId, true, "Parent");
                    schedule.Syllabus = await _syllabusRepository.GetAsync(x => x.Id == schedule.SyllabusId);
                }

                if (getAll)
                {
                    list = result
                    .OrderBy(x => x.ScheduleDate.Date)
                    .ThenBy(x => x.Start)
                    .ToList();
                }
                else
                {
                    list = result
                    .Where(x => x.StudentProfile.Status == SD.StudentProfileStatus.Teaching || x.StudentProfile.Status == SD.StudentProfileStatus.Pending)
                    .OrderBy(x => x.ScheduleDate.Date)
                    .ThenBy(x => x.Start)
                    .ToList();
                }             

                var model = new ListScheduleDTO();
                var allTutorSchedule = await _scheduleRepository.GetAllNotPagingAsync(x => !x.IsHidden && studentProfileId != 0 ? (x.StudentProfileId == studentProfileId) : (x.TutorId.Equals(tutorId)));
                if (allTutorSchedule.list != null && allTutorSchedule.list.Any())
                {
                    model.MaxDate = allTutorSchedule.list.Max(x => x.ScheduleDate.Date).Date;
                }
                model.Schedules = _mapper.Map<List<ScheduleDTO>>(list);

                _response.Result = model;
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

        [HttpPut("ChangeScheduleDateTime")]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> ChangeScheduleDateTime(ScheduleDateTimeUpdateDTO updateDTO)
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
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.SCHEDULE) };
                    return BadRequest(_response);
                }

                var originalSchedule = await _scheduleRepository.GetAsync(x => x.Id == updateDTO.Id && x.TutorId == userId);
                if (originalSchedule == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.SCHEDULE) };
                    return NotFound(_response);
                }
                originalSchedule.IsHidden = true;
                originalSchedule.UpdatedDate = DateTime.Now;
                await _scheduleRepository.UpdateAsync(originalSchedule);

                Schedule newSchedule = new()
                {
                    TutorId = originalSchedule.TutorId,
                    StudentProfileId = originalSchedule.StudentProfileId,
                    ScheduleTimeSlotId = originalSchedule.ScheduleTimeSlotId,
                    AttendanceStatus = originalSchedule.AttendanceStatus,
                    PassingStatus = originalSchedule.PassingStatus,
                    Note = originalSchedule.Note,
                    SyllabusId = originalSchedule.SyllabusId,
                    ExerciseTypeId = originalSchedule.ExerciseTypeId,
                    ExerciseId = originalSchedule.ExerciseId,
                    IsHidden = false,
                    IsUpdatedSchedule = true,
                    ScheduleDate = updateDTO.ScheduleDate,
                    Start = updateDTO.Start,
                    End = updateDTO.End,
                    CreatedDate = DateTime.Now,
                };
                newSchedule = await _scheduleRepository.CreateAsync(newSchedule);

                _response.Result = _mapper.Map<ScheduleDTO>(newSchedule);
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

        [HttpGet("GetAllAssignedSchedule")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllAssignedSchedule([FromQuery] int studentProfileId, string? status = SD.STATUS_ALL, DateTime? startDate = null, DateTime? endDate = null, string? sort = SD.ORDER_DESC, int pageNumber = 1)
        {
            try
            {
                int totalCount = 0;
                List<Schedule> list = new();
                Expression<Func<Schedule, bool>> filter = u => true;
                bool isDesc = sort != null && sort == SD.ORDER_DESC;

                var tutorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(tutorId))
                {
                    
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                if (studentProfileId != 0)
                {
                    filter = u => u.StudentProfileId == studentProfileId && !u.IsHidden && u.ExerciseId != null;
                }
                else
                {
                    filter = u => u.TutorId.Equals(tutorId) && !u.IsHidden && u.ExerciseId != null;
                }

                if (startDate != null)
                {
                    filter = filter.AndAlso(u => u.ScheduleDate.Date >= startDate.Value.Date);
                }
                if (endDate != null)
                {
                    filter = filter.AndAlso(u => u.ScheduleDate.Date <= endDate.Value.Date);
                }

                if (!string.IsNullOrEmpty(status) && status != SD.STATUS_ALL)
                {
                    switch (status.ToLower())
                    {
                        case "not_yet":
                            filter = filter.AndAlso(x => x.PassingStatus == SD.PassingStatus.NOT_YET);
                            break;
                        case "not_passed":
                            filter = filter.AndAlso(x => x.PassingStatus == SD.PassingStatus.NOT_PASSED);
                            break;
                        case "passed":
                            filter = filter.AndAlso(x => x.PassingStatus == SD.PassingStatus.PASSED);
                            break;
                    }
                }

                var (count, result) = await _scheduleRepository.GetAllWithIncludeAsync(filter: filter,
                                                                                       includeProperties: "StudentProfile,Exercise,ExerciseType",
                                                                                       pageSize: pageSize,
                                                                                       pageNumber: pageNumber,
                                                                                       orderBy: x => x.ScheduleDate,
                                                                                       isDesc: isDesc);                
                list = result;
                totalCount = count;
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = totalCount };

                _response.Result = _mapper.Map<List<ScheduleDTO>>(list);
                _response.Pagination = pagination;
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
    }
}
