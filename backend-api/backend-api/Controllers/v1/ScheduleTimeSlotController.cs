using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Repository.IRepository;
using backend_api.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

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
        public async Task<ActionResult<APIResponse>> RemoveTimeSlot(int timeSlotId)
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
                //model.StudentProfile = await _studentProfileRepository.GetAsync(x => x.Id == model.StudentProfileId, true, "Child");
                model.UpdatedDate = DateTime.Now;
                await _scheduleTimeSlotRepository.UpdateAsync(model);
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
