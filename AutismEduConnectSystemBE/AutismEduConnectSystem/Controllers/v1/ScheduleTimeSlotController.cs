using AutoMapper;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Models.DTOs;
using AutismEduConnectSystem.Models.DTOs.CreateDTOs;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace AutismEduConnectSystem.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class ScheduleTimeSlotController : ControllerBase
    {
        private readonly IScheduleRepository _scheduleRepository;
        private readonly IScheduleTimeSlotRepository _scheduleTimeSlotRepository;
        private readonly IStudentProfileRepository _studentProfileRepository;
        private readonly IResourceService _resourceService;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        private readonly ILogger<ScheduleTimeSlotController> _logger;   

        public ScheduleTimeSlotController(IScheduleRepository scheduleRepository, IScheduleTimeSlotRepository scheduleTimeSlotRepository
            , IStudentProfileRepository studentProfileRepository, IResourceService resourceService, IMapper mapper, ILogger<ScheduleTimeSlotController> logger)
        {
            _scheduleRepository = scheduleRepository;
            _scheduleTimeSlotRepository = scheduleTimeSlotRepository;
            _studentProfileRepository = studentProfileRepository;
            _resourceService = resourceService;
            _mapper = mapper;
            _response = new APIResponse();
            _logger = logger;
        }

        [HttpDelete("{timeSlotId}")]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> DeleteAsync(int timeSlotId)
        {
            try
            {
                var model = await _scheduleTimeSlotRepository.GetAsync(x => x.Id == timeSlotId);

                if (model == null)
                {
                    _logger.LogWarning("Schedule time slot with Id: {TimeSlotId} not found", timeSlotId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.SCHEDULE) };
                    return BadRequest(_response);
                }
                model.UpdatedDate = DateTime.Now;
                model.IsDeleted = true;
                await _scheduleTimeSlotRepository.UpdateAsync(model);

                var dayTillSunday = ((int)DayOfWeek.Sunday - (int)DateTime.Now.DayOfWeek + 7) % 7;
                DateTime lastDayOfWeek = DateTime.Now.AddDays(dayTillSunday);

                var scheduleToRemove = await _scheduleRepository.GetAllNotPagingAsync(x => x.StudentProfileId == model.StudentProfileId 
                                                                                        && x.ScheduleTimeSlotId == timeSlotId 
                                                                                        && x.ScheduleDate > lastDayOfWeek);
                foreach (var schedule in scheduleToRemove.list)
                {
                    await _scheduleRepository.RemoveAsync(schedule);
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting schedule time slot with Id: {TimeSlotId}", timeSlotId);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost("{studentProfileId}")]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> CreateAsync([FromBody] List<ScheduleTimeSlotCreateDTO> createDTOs, int studentProfileId)
        {
            try
            {
                var tutorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                List<ScheduleTimeSlot> scheduleTimeSlot = _mapper.Map<List<ScheduleTimeSlot>>(createDTOs);
                List<ScheduleTimeSlot> createdTimeSlots = new();
                foreach (var slot in scheduleTimeSlot)
                {
                    var isTimeSlotDuplicate = scheduleTimeSlot.Where(x => x != slot 
                                                                       && x.Weekday == slot.Weekday 
                                                                       && !(slot.To <= x.From || slot.From >= x.To)).FirstOrDefault();
                    if (isTimeSlotDuplicate == null)
                    {
                        if (slot.From >= slot.To)
                        {
                            _logger.LogWarning("Invalid time range for Slot: From: {From}, To: {To}", slot.From.ToString(@"hh\:mm"), slot.To.ToString(@"hh\:mm"));
                            _response.StatusCode = HttpStatusCode.BadRequest;
                            _response.IsSuccess = false;
                            _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.TIME_SLOT) };
                            return BadRequest(_response);
                        }

                        isTimeSlotDuplicate = await _scheduleTimeSlotRepository.GetAsync(x => x.Weekday == slot.Weekday 
                                                                                           && x.StudentProfile.TutorId.Equals(tutorId) 
                                                                                           && !(slot.To <= x.From || slot.From >= x.To) 
                                                                                           && !x.IsDeleted 
                                                                                           && (x.StudentProfile.Status == SD.StudentProfileStatus.Pending 
                                                                                           || x.StudentProfile.Status == SD.StudentProfileStatus.Teaching)
                                                                                           , true, "StudentProfile");
                        if (isTimeSlotDuplicate != null)
                        {
                            _logger.LogWarning("Duplicate time slot found: From: {From}, To: {To}", isTimeSlotDuplicate.From.ToString(@"hh\:mm"), isTimeSlotDuplicate.To.ToString(@"hh\:mm"));
                            _response.StatusCode = HttpStatusCode.BadRequest;
                            _response.IsSuccess = false;
                            _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.TIMESLOT_DUPLICATED_MESSAGE, SD.TIME_SLOT, isTimeSlotDuplicate.From.ToString(@"hh\:mm"), isTimeSlotDuplicate.To.ToString(@"hh\:mm")) };
                            return BadRequest(_response);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Duplicate time slot found in the list: From: {From}, To: {To}", isTimeSlotDuplicate.From.ToString(@"hh\:mm"), isTimeSlotDuplicate.To.ToString(@"hh\:mm"));
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.TIMESLOT_DUPLICATED_MESSAGE, SD.TIME_SLOT, isTimeSlotDuplicate.From.ToString(@"hh\:mm"), isTimeSlotDuplicate.To.ToString(@"hh\:mm")) };
                        return BadRequest(_response);
                    }

                    var daysTillNextWeek = ((int)DayOfWeek.Monday - (int)DateTime.Today.DayOfWeek + 7) % 7;
                    DateTime timeTillApply = daysTillNextWeek == 0? DateTime.Today.AddDays(7) : DateTime.Today.AddDays(daysTillNextWeek);

                    slot.StudentProfileId = studentProfileId;
                    slot.IsDeleted = false;
                    slot.CreatedDate = DateTime.Now;
                    slot.AppliedDate = timeTillApply.Date;
                    var newTimeSLot = await _scheduleTimeSlotRepository.CreateAsync(slot);
                    createdTimeSlots.Add(newTimeSLot);

                    // Generate next week schedule
                    DateTime nextWeekSchedule = slot.Weekday == 0 ? timeTillApply : timeTillApply.AddDays(slot.Weekday - 1);

                    Schedule schedule = new Schedule()
                    {
                        TutorId = tutorId,
                        AttendanceStatus = SD.AttendanceStatus.NOT_YET,
                        ScheduleDate = nextWeekSchedule,
                        StudentProfileId = studentProfileId,
                        CreatedDate = DateTime.Now,
                        PassingStatus = SD.PassingStatus.NOT_YET,
                        UpdatedDate = DateTime.Now,
                        Start = slot.From,
                        End = slot.To,
                        ScheduleTimeSlotId = newTimeSLot.Id
                    };
                    await _scheduleRepository.CreateAsync(schedule);
                }
                
                _response.Result = _mapper.Map<List<ScheduleTimeSlotDTO>>(createdTimeSlots);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating time slots for studentProfileId: {StudentProfileId}", studentProfileId);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
