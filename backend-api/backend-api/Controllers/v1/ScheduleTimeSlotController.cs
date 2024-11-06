using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Repository.IRepository;
using backend_api.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace backend_api.Controllers.v1
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

        public ScheduleTimeSlotController(IScheduleRepository scheduleRepository, IScheduleTimeSlotRepository scheduleTimeSlotRepository
            , IStudentProfileRepository studentProfileRepository, IResourceService resourceService, IMapper mapper)
        {
            _scheduleRepository = scheduleRepository;
            _scheduleTimeSlotRepository = scheduleTimeSlotRepository;
            _studentProfileRepository = studentProfileRepository;
            _resourceService = resourceService;
            _mapper = mapper;
            _response = new APIResponse();
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
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost("{studentProfileId}")]
        //[Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> CreateAsync([FromBody] List<ScheduleTimeSlotCreateDTO> createDTOs, int studentProfileId)
        {
            try
            {
                var tutorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                List<ScheduleTimeSlot> scheduleTimeSlot = _mapper.Map<List<ScheduleTimeSlot>>(createDTOs);
                foreach (var slot in scheduleTimeSlot)
                {
                    var isTimeSlotDuplicate = scheduleTimeSlot.Where(x => x != slot 
                                                                       && x.Weekday == slot.Weekday 
                                                                       && !(slot.To <= x.From || slot.From >= x.To)).FirstOrDefault();
                    if (isTimeSlotDuplicate == null)
                    {
                        if (slot.From >= slot.To)
                        {
                            _response.StatusCode = HttpStatusCode.BadRequest;
                            _response.IsSuccess = false;
                            _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.TIME_SLOT) };
                            return BadRequest(_response);
                        }

                        isTimeSlotDuplicate = await _scheduleTimeSlotRepository.GetAsync(x => x.Weekday == slot.Weekday 
                                                                                           && x.StudentProfile.TutorId.Equals(tutorId) 
                                                                                           && !(slot.To <= x.From || slot.From >= x.To) 
                                                                                           //&& !x.IsDeleted 
                                                                                           && (x.StudentProfile.Status == SD.StudentProfileStatus.Pending 
                                                                                           || x.StudentProfile.Status == SD.StudentProfileStatus.Teaching)
                                                                                           , true, "StudentProfile");
                        if (isTimeSlotDuplicate != null)
                        {
                            _response.StatusCode = HttpStatusCode.BadRequest;
                            _response.IsSuccess = false;
                            _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.TIMESLOT_DUPLICATED_MESSAGE, SD.TIME_SLOT, isTimeSlotDuplicate.From.ToString(@"hh\:mm"), isTimeSlotDuplicate.To.ToString(@"hh\:mm")) };
                            return BadRequest(_response);
                        }
                    }
                    else
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.TIMESLOT_DUPLICATED_MESSAGE, SD.TIME_SLOT, isTimeSlotDuplicate.From.ToString(@"hh\:mm"), isTimeSlotDuplicate.To.ToString(@"hh\:mm")) };
                        return BadRequest(_response);
                    }

                    slot.StudentProfileId = studentProfileId;
                    slot.IsDeleted = false;
                    slot.CreatedDate = DateTime.Now;
                    await _scheduleTimeSlotRepository.CreateAsync(slot);
                }

                // TODO: gen lich neu can
                
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
