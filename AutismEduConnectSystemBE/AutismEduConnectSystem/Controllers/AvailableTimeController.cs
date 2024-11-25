using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Models.DTOs;
using AutismEduConnectSystem.Models.DTOs.CreateDTOs;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace AutismEduConnectSystem.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersionNeutral]
    public class AvailableTimeController : ControllerBase
    {
        private readonly IAvailableTimeSlotRepository _availableTimeSlotRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        protected ILogger<AvailableTimeController> _logger;
        private readonly IResourceService _resourceService;

        public AvailableTimeController(IAvailableTimeSlotRepository availableTimeSlotRepository, IMapper mapper,
            IResourceService resourceService, ILogger<AvailableTimeController> logger)
        {
            _resourceService = resourceService;
            _logger = logger;
            _availableTimeSlotRepository = availableTimeSlotRepository;
            _response = new APIResponse();
            _mapper = mapper;
        }

        [HttpPost]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> CreateAsync([FromBody] AvailableTimeSlotCreateDTO availableTimeSlotCreateDTO)
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
                
                if (availableTimeSlotCreateDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _logger.LogError("Bad request: AvailableTimeSlotCreateDTO is null or Tutor ID is missing.");
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.AVAILABLE_TIME) };
                    return BadRequest(_response);
                }

                if (TimeSpan.Parse(availableTimeSlotCreateDTO.From) >= TimeSpan.Parse(availableTimeSlotCreateDTO.To))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _logger.LogError("Bad request: 'From' time ({FromTime}) is later than 'To' time ({ToTime}).", availableTimeSlotCreateDTO.From, availableTimeSlotCreateDTO.To);
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.TIME_SLOT) };
                    return BadRequest(_response);
                }

                AvailableTimeSlot availableTimeSlot = _mapper.Map<AvailableTimeSlot>(availableTimeSlotCreateDTO);

                var existingTimeSlots = await _availableTimeSlotRepository.GetAllNotPagingAsync(x => x.Weekday == availableTimeSlot.Id, null, null);
                if (existingTimeSlots.list != null && existingTimeSlots.list.Count > 0)
                {
                    foreach (var slot in existingTimeSlots.list)
                    {
                        if (availableTimeSlot.From >= slot.From && availableTimeSlot.From < slot.To ||
                            availableTimeSlot.To > slot.From && availableTimeSlot.To <= slot.To ||
                            availableTimeSlot.From <= slot.From && availableTimeSlot.To >= slot.To)
                        {
                            _response.StatusCode = HttpStatusCode.BadRequest;
                            _response.IsSuccess = false;
                            _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.TIMESLOT_DUPLICATED_MESSAGE, SD.TIME_SLOT, slot.From.ToString(@"hh\:mm"), slot.To.ToString(@"hh\:mm")) };
                            return BadRequest(_response);
                        }
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
                _logger.LogError("Error occurred while creating an assessment question: {Message}", ex.Message);
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllTimeSlotFromWeekday([FromQuery] string tutorId, int weekday)
        {
            try
            {

                var existingTimeSlots = await _availableTimeSlotRepository.GetAllNotPagingAsync(x => x.Weekday == weekday && x.TutorId.Equals(tutorId), null, null);
                _response.Result = _mapper.Map<List<AvailableTimeSlotDTO>>(existingTimeSlots.list.OrderBy(x => x.From));
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _logger.LogError("Error occurred while creating an assessment question: {Message}", ex.Message);
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpDelete("{timeslotId}")]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> RemoveTimeSlotFromWeekday(int timeSlotId)
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
                
                var timeslot = await _availableTimeSlotRepository.GetAsync(x => x.Id == timeSlotId && x.TutorId == tutorId, true, null);
                if (timeslot == null)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.AVAILABLE_TIME) };
                    return NotFound(_response);
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
                _logger.LogError("Error occurred while creating an assessment question: {Message}", ex.Message);
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
