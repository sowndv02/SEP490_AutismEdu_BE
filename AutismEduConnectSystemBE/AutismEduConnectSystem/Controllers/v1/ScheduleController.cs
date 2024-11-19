using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Models.DTOs;
using AutismEduConnectSystem.Models.DTOs.UpdateDTOs;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.Utils;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public ScheduleController(IScheduleRepository scheduleRepository, IMapper mapper
            , IStudentProfileRepository studentProfileRepository, IResourceService resourceService,
            IChildInformationRepository childInfoRepository, ISyllabusRepository syllabusRepository, 
            ILogger<ScheduleController> logger, IConfiguration configuration)
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
                    _logger.LogWarning("Unauthorized access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                var model = await _scheduleRepository.GetAsync(x => x.Id == id, false, "Exercise,ExerciseType");
                if (model == null)
                {
                    _logger.LogWarning("Schedule with ID: {ScheduleId} not found.", id);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.SCHEDULE) };
                    return BadRequest(_response);
                }
                model.StudentProfile = await _studentProfileRepository.GetAsync(x => x.Id == model.StudentProfileId, true, "Child");
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = _mapper.Map<ScheduleDTO>(model);
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving schedule with ID: {ScheduleId}", id);
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
                if (updateDTO == null || updateDTO.Id != id)
                {
                    _logger.LogWarning("Invalid update data or ID mismatch for Schedule ID: {ScheduleId}", id);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.SCHEDULE) };
                    return BadRequest(_response);
                }
                var model = await _scheduleRepository.GetAsync(x => x.Id == id, false, null);
                if (model == null)
                {
                    _logger.LogWarning("Schedule with ID: {ScheduleId} not found for update.", id);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.SCHEDULE) };
                    return BadRequest(_response);
                }
                model.ExerciseId = updateDTO.ExerciseId;
                model.ExerciseTypeId = updateDTO.ExerciseTypeId;
                model.SyllabusId = updateDTO.SyllabusId;
                model.UpdatedDate = DateTime.Now;
                await _scheduleRepository.UpdateAsync(model);
                var result = await _scheduleRepository.GetAsync(x => x.Id == id, false, "ExerciseType,Exercise");
                result.StudentProfile = await _studentProfileRepository.GetAsync(x => x.Id == model.StudentProfileId, true, "Child");
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                _response.Result = _mapper.Map<ScheduleDTO>(result);
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating schedule with ID: {ScheduleId}", id);
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
                    _logger.LogWarning("Unauthorized access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                List<Schedule> list = new();

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
                _logger.LogError(ex, "Error occurred while retrieving failed exercises for Tutor ID: {TutorId} and StudentProfile ID: {StudentProfileId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, studentProfileId);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPut("changeStatus/{id}")]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> UpdateStatusAsync(int id, [FromBody] ScheduleUpdateDTO updateDTO)
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
                
                if (updateDTO == null || updateDTO.Id != id)
                {
                    _logger.LogWarning("Invalid update request: Schedule ID mismatch or empty request body for Schedule ID: {ScheduleId}", id);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.SCHEDULE) };
                    return BadRequest(_response);
                }
                var model = await _scheduleRepository.GetAsync(x => x.Id == id && x.TutorId == userId, false, "Exercise,ExerciseType");
                if (model == null)
                {
                    _logger.LogWarning("Schedule not found: {ScheduleId}", id);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.SCHEDULE) };
                    return BadRequest(_response);
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
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                _response.Result = _mapper.Map<ScheduleDTO>(result);
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating Schedule ID: {ScheduleId} by Tutor ID: {TutorId}", id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] int studentProfileId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                List<Schedule> list = new();
                Expression<Func<Schedule, bool>> filter = u => true;

                var tutorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(tutorId))
                {
                    _logger.LogWarning("Unauthorized access attempt detected.");
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

                list = result
                    .OrderBy(x => x.ScheduleDate.Date)
                    .ThenBy(x => x.Start)
                    .ToList();

                var model = new ListScheduleDTO();
                var allTutorSchedule = await _scheduleRepository.GetAllNotPagingAsync(x => !x.IsHidden && studentProfileId != 0 ? (x.StudentProfileId == studentProfileId) : (x.TutorId.Equals(tutorId)));
                if (allTutorSchedule.list.Any())
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
                _logger.LogError(ex, "Error occurred while fetching schedules for Tutor ID: {TutorId} with StudentProfileId: {StudentProfileId}, StartDate: {StartDate}, EndDate: {EndDate}",
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value, studentProfileId, startDate, endDate);
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
                
                if (updateDTO == null)
                {
                    _logger.LogWarning("Received null ScheduleDateTimeUpdateDTO");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.SCHEDULE) };
                    return BadRequest(_response);
                }

                var originalSchedule = await _scheduleRepository.GetAsync(x => x.Id == updateDTO.Id && x.TutorId == userId);
                if (originalSchedule == null)
                {
                    _logger.LogWarning("Schedule with Id: {ScheduleId} not found", updateDTO.Id);

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
                _logger.LogError(ex, "Error occurred while changing schedule date and time for ScheduleId: {ScheduleId}", updateDTO?.Id);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet("GetAllAssignedSchedule")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllAssignedSchedule([FromQuery] int studentProfileId, string? sort = SD.ORDER_DESC, int pageNumber = 1)
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
                    _logger.LogWarning("Unauthorized access attempt detected.");
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

                var (count, result) = await _scheduleRepository.GetAllWithIncludeAsync(filter: filter,
                                                                                       includeProperties: "StudentProfile,Exercise,ExerciseType",
                                                                                       pageSize: pageSize,
                                                                                       pageNumber: pageNumber,
                                                                                       orderBy: x => x.ScheduleDate,
                                                                                       isDesc: isDesc);
                foreach (Schedule schedule in result)
                {
                    schedule.StudentProfile = await _studentProfileRepository.GetAsync(x => x.Id == schedule.StudentProfileId);
                    schedule.StudentProfile.Child = await _childInfoRepository.GetAsync(x => x.Id == schedule.StudentProfile.ChildId, true, "Parent");
                    schedule.Syllabus = await _syllabusRepository.GetAsync(x => x.Id == schedule.SyllabusId);
                }

                list = result
                    .OrderBy(x => x.ScheduleDate.Date)
                    .ThenBy(x => x.Start)
                    .ToList();
                totalCount = count;
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = totalCount };

                var model = new ListScheduleDTO();
                var allTutorSchedule = await _scheduleRepository.GetAllNotPagingAsync(x => !x.IsHidden && studentProfileId != 0 ? (x.StudentProfileId == studentProfileId) : (x.TutorId.Equals(tutorId)));
                model.MaxDate = allTutorSchedule.list.Max(x => x.ScheduleDate.Date).Date;
                model.Schedules = _mapper.Map<List<ScheduleDTO>>(list);

                _response.Result = model;
                _response.Pagination = pagination;
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching schedules for Tutor ID: {TutorId} with StudentProfileId: {StudentProfileId}",
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value, studentProfileId);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
