using AutoMapper;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models.DTOs.UpdateDTOs;
using backend_api.Models.DTOs;
using backend_api.Models;
using backend_api.RabbitMQSender;
using backend_api.Repository.IRepository;
using backend_api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static backend_api.SD;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using backend_api.Repository;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class SyllabusController : ControllerBase
    {
        private readonly ISyllabusRepository _syllabusRepository;
        private readonly IExerciseRepository _exerciseRepository;
        private readonly ISyllabusExerciseRepository _syllabusExerciseRepository;
        private readonly IExerciseTypeRepository _exerciseTypeRepository;
        private readonly ILogger<SyllabusController> _logger;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        protected int pageSize = 0;
        public SyllabusController(ISyllabusRepository syllabusRepository, ILogger<SyllabusController> logger, IMapper mapper, 
            IConfiguration configuration, IExerciseRepository exerciseRepository, IExerciseTypeRepository exerciseTypeRepository,
            ISyllabusExerciseRepository syllabusExerciseRepository)
        {
            _syllabusExerciseRepository = syllabusExerciseRepository;
            _exerciseTypeRepository = exerciseTypeRepository;   
            _exerciseRepository = exerciseRepository;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _response = new APIResponse();
            _mapper = mapper;
            _logger = logger;
            _syllabusRepository = syllabusRepository;
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> DeleteAsync(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }
                if (id == 0)
                {
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }
                var model = await _syllabusRepository.GetAsync(x => x.Id == id && x.TutorId == userId, false, null);

                if (model == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }
                model.IsDeleted = true;
                await _syllabusRepository.UpdateAsync(model);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);

            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
            }
            return _response;
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery]string? orderBy = SD.AGE_FROM, string? sort = SD.ORDER_DESC, int pageNumber = 1)
        {
            try
            {
                int totalCount = 0;
                List<Syllabus> list = new();
                Expression<Func<Syllabus, bool>> filter = u => true;
                Expression<Func<Syllabus, object>> orderByQuery = u => true;
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                if (userRoles.Contains(SD.TUTOR_ROLE))
                {
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    Expression<Func<Syllabus, bool>> searchByTutor = u => !string.IsNullOrEmpty(u.TutorId) && u.TutorId == userId && !u.IsDeleted;

                    var combinedFilter = Expression.Lambda<Func<Syllabus, bool>>(
                        Expression.AndAlso(filter.Body, Expression.Invoke(searchByTutor, filter.Parameters)),
                        filter.Parameters
                    );
                    filter = combinedFilter;
                }
                bool isDesc = !string.IsNullOrEmpty(sort) && sort == SD.ORDER_DESC;
                if (orderBy != null)
                {
                    switch (orderBy)
                    {
                        case SD.CREADTED_DATE:
                            orderByQuery = x => x.CreatedDate;
                            break;
                        case SD.AGE_FROM:
                            orderByQuery = x => x.AgeFrom;
                            break;
                        default:
                            orderByQuery = x => x.CreatedDate;
                            break;
                    }
                }
                var (count, result) = await _syllabusRepository.GetAllAsync(filter, "SyllabusExercises", pageSize: pageSize, pageNumber: pageNumber, orderByQuery, isDesc);
                list = result;
                totalCount = count;
                foreach (var item in list)
                {
                    var (syllabusExerciseCount, syllabusExercises) = await _syllabusExerciseRepository.GetAllNotPagingAsync(filter: x => x.SyllabusId == item.Id, includeProperties: "Exercise,ExerciseType", excludeProperties: null);
                    item.SyllabusExercises = syllabusExercises;
                    item.ExerciseTypes = syllabusExercises
                    .GroupBy(se => se.ExerciseTypeId)
                    .Select(group => new ExerciseTypeDTO
                    {
                        Id = group.First().ExerciseType.Id,
                        ExerciseTypeName = group.First().ExerciseType.ExerciseTypeName,
                        Exercises = group.Select(g => new ExerciseDTO
                        {
                            Id = g.Exercise.Id,
                            ExerciseName = g.Exercise.ExerciseName,
                        }).ToList()
                    }).ToList();
                }

                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = totalCount };
                var reutnResult = _mapper.Map<List<SyllabusDTO>>(list);
                _response.Result = reutnResult;
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;
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



        [HttpGet("{id}")]
        public async Task<IActionResult> GetActive(int id)
        {
            var model = await _syllabusRepository.GetAsync(x => x.Id == id && !x.IsDeleted);
            if (model == null)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                return BadRequest(_response);
            }
            var (syllabusExerciseCount, syllabusExercises) = await _syllabusExerciseRepository.GetAllNotPagingAsync(filter: x => x.SyllabusId == model.Id, includeProperties: "Exercise,ExerciseType", excludeProperties: null);
            model.SyllabusExercises = syllabusExercises;
            _response.StatusCode = HttpStatusCode.Created;
            _response.Result = _mapper.Map<SyllabusDTO>(model);
            _response.IsSuccess = true;
            return Ok(_response);
        }


        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateAsync(int id,[FromBody]SyllabusUpdateDTO updateDTO)
        {
            if (!ModelState.IsValid || id != updateDTO.Id)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                return BadRequest(_response);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var model = await _syllabusRepository.GetAsync(x => x.Id == id && x.TutorId == userId, false, null, null);  
            model.AgeEnd = updateDTO.AgeEnd;
            model.AgeFrom = updateDTO.AgeFrom;
            model.UpdatedDate = DateTime.Now;
            foreach(var exerciseTypeUpdate in updateDTO.SyllabusExercises)
            {
                int exerciseTypeId = exerciseTypeUpdate.ExerciseTypeId;
                List<int> exerciseIds = exerciseTypeUpdate.ExerciseIds;
                var existingSyllabusExercises = await _syllabusExerciseRepository.GetAllAsync(se => se.SyllabusId == id && se.ExerciseTypeId == exerciseTypeId);
                var exercisesToDelete = existingSyllabusExercises.list
                    .Where(se => !exerciseIds.Contains(se.ExerciseId))
                    .ToList();
                var existingExerciseIds = existingSyllabusExercises.list.Select(se => se.ExerciseId).ToList();
                var exercisesToAdd = exerciseIds
                    .Where(id => !existingExerciseIds.Contains(id))
                    .Select(id => new SyllabusExercise
                    {
                        SyllabusId = id,
                        ExerciseTypeId = exerciseTypeId,
                        ExerciseId = id,
                        CreatedDate = DateTime.Now
                    })
                    .ToList();
                foreach (var deleteExercise in exercisesToDelete) 
                {
                    await _syllabusExerciseRepository.RemoveAsync(deleteExercise);
                }
                foreach(var addExercise in exercisesToAdd)
                {
                    await _syllabusExerciseRepository.CreateAsync(addExercise);
                }
            }

            _response.StatusCode = HttpStatusCode.Created;
            _response.Result = _mapper.Map<SyllabusDTO>(model);
            _response.IsSuccess = true;
            return Ok(_response);
        }

        [HttpPost]
        //[Authorize]
        public async Task<IActionResult> CreateAsync(SyllabusCreateDTO createDTO)
        {
            if (!ModelState.IsValid)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                return BadRequest(_response);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var syllabus = _mapper.Map<Syllabus>(createDTO);
            syllabus.TutorId = userId;
            var model = await _syllabusRepository.CreateAsync(syllabus);
            foreach (var item in createDTO.SyllabusExercises)
            {
                foreach (var exercise in item.ExerciseIds) 
                {
                    await _syllabusExerciseRepository.CreateAsync(new SyllabusExercise()
                    {
                        ExerciseId = exercise,
                        ExerciseTypeId = item.ExerciseTypeId,
                        SyllabusId = model.Id
                    });
                }
            }

            _response.StatusCode = HttpStatusCode.Created;
            _response.Result = _mapper.Map<SyllabusDTO>(model);
            _response.IsSuccess = true;
            return Ok(_response);
        }

    }
}
