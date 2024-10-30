using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models.DTOs;
using backend_api.Repository;
using backend_api.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using System.Linq.Expressions;
using backend_api.Utils;

namespace backend_api.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class ScheduleController : ControllerBase
    {
        private readonly IScheduleRepository _scheduleRepository;
        private readonly IStudentProfileRepository _studentProfileRepository;

        protected APIResponse _response;
        private readonly IMapper _mapper;

        public ScheduleController(IScheduleRepository scheduleRepository, IMapper mapper
            , IStudentProfileRepository studentProfileRepository)
        {
            _scheduleRepository = scheduleRepository;
            _response = new APIResponse();
            _mapper = mapper;
            _studentProfileRepository = studentProfileRepository;
        }



        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] int studentProfileId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                int totalCount = 0;
                List<Schedule> list = new();
                Expression<Func<Schedule, bool>> filter = u => true;

                if(studentProfileId != 0)
                {
                    filter = u => u.StudentProfileId == studentProfileId;
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
                                "StudentProfile", null, null, true);
                
                foreach (Schedule schedule in result)
                {
                    schedule.StudentProfile = await _studentProfileRepository.GetAsync(x => x.Id == schedule.StudentProfileId,true,"Child");
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
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
