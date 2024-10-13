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
        private readonly IAvailableTimeSlotRepository _availableTimeSlotRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;

        public AvailableTimeController(IAvailableTimeSlotRepository availableTimeSlotRepository, IMapper mapper)
        {
            _availableTimeSlotRepository = availableTimeSlotRepository;
            _response = new APIResponse();
            _mapper = mapper;
        }

        [HttpPost]
        //[Authorize]
        public async Task<ActionResult<APIResponse>> CreateAsync([FromBody] AvailableTimeSlotCreateDTO availableTimeSlotCreateDTO)
        {
            try
            {
                var tutorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (availableTimeSlotCreateDTO == null || string.IsNullOrEmpty(tutorId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }
                if (TimeSpan.Parse(availableTimeSlotCreateDTO.From) > TimeSpan.Parse(availableTimeSlotCreateDTO.To))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.TIMESLOT_INVALID };
                    return BadRequest(_response);
                }

                AvailableTimeSlot availableTimeSlot = _mapper.Map<AvailableTimeSlot>(availableTimeSlotCreateDTO);
                
                var existingTimeSlots = await _availableTimeSlotRepository.GetAllNotPagingAsync(x => x.Weekday == availableTimeSlot.Id, null, null);
                foreach (var slot in existingTimeSlots.list)
                {
                    if (!(availableTimeSlot.To <= slot.From || availableTimeSlot.From >= slot.To))
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        _response.ErrorMessages = new List<string> { $"{SD.TIMESLOT_DUPLICATED} {slot.From.ToString(@"hh\:mm")}-{slot.To.ToString(@"hh\:mm")}" };
                        return BadRequest(_response);
                    }
                }

                availableTimeSlot.TutorId = tutorId;
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
        public async Task<ActionResult<APIResponse>> GetAllTimeSlotFromWeekday([FromQuery] string tutorId,int weekday)
        {
            try
            {

                if (string.IsNullOrEmpty(tutorId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }
                
                var existingTimeSlots = await _availableTimeSlotRepository.GetAllNotPagingAsync(x => x.Weekday == weekday && x.TutorId.Equals(tutorId), null, null);
                
                //if (existingTimeSlots.list == null)
                //{
                //    _response.Result = null;
                //    _response.StatusCode = HttpStatusCode.OK;
                //    return Ok(_response);
                //}

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

        [HttpDelete("{timeslotId}")]
        //[Authorize]
        public async Task<ActionResult<APIResponse>> RemoveTimeSlotFromWeekday(int timeSlotId)
        {
            try
            {
                var timeslot = await _availableTimeSlotRepository.GetAsync(x => x.Id == timeSlotId, true, null);
                if(timeslot == null)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.InternalServerError;
                    _response.ErrorMessages = new List<string>() { SD.NOT_FOUND_MESSAGE };
                    return StatusCode((int)HttpStatusCode.InternalServerError, _response);
                }
                AvailableTimeSlot model = _mapper.Map<AvailableTimeSlot>(timeslot);
                await _availableTimeSlotRepository.RemoveAsync(model);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
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
