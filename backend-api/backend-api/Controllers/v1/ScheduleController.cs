using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.UpdateDTOs;
using backend_api.Repository.IRepository;
using backend_api.Services.IServices;
using backend_api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;

namespace backend_api.Controllers.v1
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

        public ScheduleController(IScheduleRepository scheduleRepository, IMapper mapper
            , IStudentProfileRepository studentProfileRepository, IResourceService resourceService,
            IChildInformationRepository childInfoRepository, ISyllabusRepository syllabusRepository)
        {
            _resourceService = resourceService;
            _scheduleRepository = scheduleRepository;
            _response = new APIResponse();
            _mapper = mapper;
            _studentProfileRepository = studentProfileRepository;
            _childInfoRepository = childInfoRepository;
            _syllabusRepository = syllabusRepository;
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetByIdAsync(int id)
        {
            try
            {
                var model = await _scheduleRepository.GetAsync(x => x.Id == id, false, "Exercise,ExerciseType");
                if (model == null)
                {
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
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }



        [HttpPut("AssignExercises/{id}")]
        public async Task<ActionResult<APIResponse>> UpdateAsync(int id, [FromBody] AssignExerciseScheduleDTO updateDTO)
        {
            try
            {
                if (updateDTO == null || updateDTO.Id != id)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.SCHEDULE) };
                    return BadRequest(_response);
                }
                var model = await _scheduleRepository.GetAsync(x => x.Id == id, false, null);
                if (model == null)
                {
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
                List<Schedule> list = new();
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

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
        public async Task<ActionResult<APIResponse>> UpdateAsync(int id, [FromBody] ScheduleUpdateDTO updateDTO)
        {
            try
            {
                if (updateDTO == null || updateDTO.Id != id)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.SCHEDULE) };
                    return BadRequest(_response);
                }
                var model = await _scheduleRepository.GetAsync(x => x.Id == id, false, "Exercise,ExerciseType");
                if (model == null)
                {
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
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] int studentProfileId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                List<Schedule> list = new();
                Expression<Func<Schedule, bool>> filter = u => true;

                if (studentProfileId != 0)
                {
                    filter = u => u.StudentProfileId == studentProfileId && !u.IsHidden;
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

                _response.Result = _mapper.Map<List<ScheduleDTO>>(list);
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
                if (updateDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.SCHEDULE) };
                    return BadRequest(_response);
                }

                var originalSchedule = await _scheduleRepository.GetAsync(x => x.Id == updateDTO.Id);
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
    }
}
