using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace backend_api.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class AvailableTimeController : ControllerBase
    {
        private readonly IAvailableTimeRepository _availableTimeRepository;
        private readonly IAvailableTimeSlotRepository _availableTimeSlotRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;

        public AvailableTimeController(IAvailableTimeRepository availableTimeRepository, IAvailableTimeSlotRepository availableTimeSlotRepository, IMapper mapper)
        {
            _availableTimeRepository = availableTimeRepository;
            _availableTimeSlotRepository = availableTimeSlotRepository;
            _response = new APIResponse();
            _mapper = mapper;
        }

        [HttpPost]
        //[Authorize]
        public async Task<ActionResult<APIResponse>> CreateAsync([FromBody] AvailableTimeCreateDTO availableTimeCreateDTO)
        {
            try
            {
                var tutorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (availableTimeCreateDTO == null || string.IsNullOrEmpty(tutorId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { $"Bad request!" };
                    return BadRequest(_response);
                }
                if (TimeSpan.Parse(availableTimeCreateDTO.TimeSlot.From) > TimeSpan.Parse(availableTimeCreateDTO.TimeSlot.To))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { $"Invalid timeslot!" };
                    return BadRequest(_response);
                }
                var availableTime = _availableTimeRepository.GetAsync(x => x.TutorId.Equals(tutorId) &&
                    x.Weekday.Equals(availableTimeCreateDTO.Weekday), true, null).GetAwaiter().GetResult();
                if (availableTime == null)
                {
                    AvailableTime model = _mapper.Map<AvailableTime>(availableTimeCreateDTO);
                    model.TutorId = tutorId;
                    model.CreatedDate = DateTime.Now;
                    availableTime = await _availableTimeRepository.CreateAsync(model);
                }
                AvailableTimeSlot availableTimeSlot = _mapper.Map<AvailableTimeSlot>(availableTimeCreateDTO.TimeSlot);
                
                var existingTimeSlots = _availableTimeSlotRepository.GetAllNotPagingAsync(x => x.WeekdayId == availableTime.Id, null, null).GetAwaiter().GetResult();
                foreach (var slot in existingTimeSlots.list)
                {
                    if (!(availableTimeSlot.To <= slot.From || availableTimeSlot.From >= slot.To))
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        _response.ErrorMessages = new List<string> { $"The new time slot overlaps with an existing time slot {slot.From.ToString(@"hh\:mm")}-{slot.To.ToString(@"hh\:mm")}" };
                        return BadRequest(_response);
                    }
                }

                availableTimeSlot.WeekdayId = availableTime.Id;
                availableTimeSlot.CreatedDate = DateTime.Now;
                await _availableTimeSlotRepository.CreateAsync(availableTimeSlot);

                _response.StatusCode = HttpStatusCode.Created;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet]
        //[Authorize]
        public async Task<ActionResult<APIResponse>> GetAllTimeSlotFromWeekday(int weekday)
        {
            try
            {
                var tutorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(tutorId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { $"Bad request!" };
                    return BadRequest(_response);
                }

                var availableTime = _availableTimeRepository.GetAsync(x => x.TutorId.Equals(tutorId) && x.Weekday == weekday, true, null).GetAwaiter().GetResult();
                if (availableTime == null)
                {
                    _response.Result = null;
                    _response.StatusCode = HttpStatusCode.OK;
                    return Ok(_response);
                }
                var existingTimeSlots = _availableTimeSlotRepository.GetAllNotPagingAsync(x => x.WeekdayId == availableTime.Id, null, null).GetAwaiter().GetResult();

                _response.Result = _mapper.Map<List<AvailableTimeSlotDTO>>(existingTimeSlots.list.OrderBy(x => x.From));
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
