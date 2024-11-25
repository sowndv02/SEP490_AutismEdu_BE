using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Models.DTOs;
using AutismEduConnectSystem.Models.DTOs.CreateDTOs;
using AutismEduConnectSystem.Models.DTOs.UpdateDTOs;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
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
        private readonly IResourceService _resourceService;

        public SyllabusController(ISyllabusRepository syllabusRepository, ILogger<SyllabusController> logger, IMapper mapper,
            IConfiguration configuration, IExerciseRepository exerciseRepository, IExerciseTypeRepository exerciseTypeRepository,
            ISyllabusExerciseRepository syllabusExerciseRepository, IResourceService resourceService)
        {
            _syllabusExerciseRepository = syllabusExerciseRepository;
            _exerciseTypeRepository = exerciseTypeRepository;
            _exerciseRepository = exerciseRepository;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _response = new APIResponse();
            _mapper = mapper;
            _logger = logger;
            _syllabusRepository = syllabusRepository;
            _resourceService = resourceService;
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> DeleteAsync(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
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
                
                if (id <= 0)
                {
                    _logger.LogWarning($"Invalid syllabus ID: {id} provided by User: {userId}");
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                var model = await _syllabusRepository.GetAsync(x => x.Id == id && x.TutorId == userId, true, null);

                if (model == null)
                {
                    _logger.LogWarning($"Syllabus ID: {id} not found for Tutor: {userId}");
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.SYLLABUS) };
                    return NotFound(_response);
                }
                model.IsDeleted = true;
                await _syllabusRepository.UpdateAsync(model);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while deleting Syllabus ID: {id} for Tutor: {User.FindFirst(ClaimTypes.NameIdentifier)?.Value}");
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
            }
            return _response;
        }

        [HttpGet]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? orderBy = SD.AGE_FROM, string? sort = SD.ORDER_DESC)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
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
                
                Expression<Func<Syllabus, bool>> filter = u => !string.IsNullOrEmpty(u.TutorId) && u.TutorId == userId && !u.IsDeleted;
                Expression<Func<Syllabus, object>> orderByQuery = u => true;
                bool isDesc = !string.IsNullOrEmpty(sort) && sort == SD.ORDER_DESC;
                if (orderBy != null)
                {
                    switch (orderBy)
                    {
                        case SD.CREATED_DATE:
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
                var (count, result) = await _syllabusRepository.GetAllNotPagingAsync(filter, includeProperties: "SyllabusExercises", excludeProperties: null, orderByQuery, isDesc);
                foreach (var item in result)
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
                            Description = g.Exercise.Description
                        }).ToList()
                    }).ToList();
                }

                var reutnResult = _mapper.Map<List<SyllabusDTO>>(result);
                _response.Result = reutnResult;
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = null;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving syllabi for Tutor: {userId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }



        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse>> GetActive(int id)
        {
            try
            {
                if(id <= 0)
                {
                    _logger.LogWarning("Syllabus not found for id: {id}", id);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                var model = await _syllabusRepository.GetAsync(x => x.Id == id && !x.IsDeleted);
                if (model == null)
                {
                    _logger.LogWarning("Syllabus not found for id: {id}", id);
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.SYLLABUS) };
                    return NotFound(_response);
                }
                var (syllabusExerciseCount, syllabusExercises) = await _syllabusExerciseRepository.GetAllNotPagingAsync(filter: x => x.SyllabusId == model.Id, includeProperties: "Exercise,ExerciseType", excludeProperties: null);
                model.SyllabusExercises = syllabusExercises;
                _response.StatusCode = HttpStatusCode.Created;
                _response.Result = _mapper.Map<SyllabusDTO>(model);
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving syllabus with id: {id}", id);

                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPut("{id}")]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> UpdateAsync(int id, [FromBody] SyllabusUpdateDTO updateDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
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
                if (!ModelState.IsValid || id != updateDTO.Id)
                {
                    _logger.LogWarning("Invalid model state or mismatched id: {id}", id);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.SYLLABUS) };
                    return BadRequest(_response);
                }

                var model = await _syllabusRepository.GetAsync(x => x.Id == id && x.TutorId == userId, false, null, null);
                if (model == null)
                {
                    _logger.LogWarning("Syllabus not found for id: {id} and tutor: {userId}", id, userId);
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.SYLLABUS) };
                    return NotFound(_response);
                }
                var (total, list) = await _syllabusRepository.GetAllNotPagingAsync(x => updateDTO.AgeFrom >= x.AgeFrom && updateDTO.AgeEnd >= x.AgeEnd && x.TutorId == userId && !x.IsDeleted && x.Id != updateDTO.Id);
                foreach (var item in list)
                {
                    if (item.AgeFrom == updateDTO.AgeFrom && item.AgeEnd == updateDTO.AgeEnd)
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.AGE) };
                        return BadRequest(_response);
                    }
                }
                model.AgeEnd = updateDTO.AgeEnd;
                model.AgeFrom = updateDTO.AgeFrom;
                model.UpdatedDate = DateTime.Now;
                await _syllabusRepository.UpdateAsync(model);
                foreach (var exerciseTypeUpdate in updateDTO.SyllabusExercises)
                {
                    int exerciseTypeId = exerciseTypeUpdate.ExerciseTypeId;
                    List<int> exerciseIds = exerciseTypeUpdate.ExerciseIds;
                    var existingSyllabusExercises = await _syllabusExerciseRepository.GetAllNotPagingAsync(se => se.SyllabusId == updateDTO.Id && se.ExerciseTypeId == exerciseTypeId, null, null, x => x.CreatedDate, true);
                    var exercisesToDelete = existingSyllabusExercises.list
                        .Where(se => !exerciseIds.Contains(se.ExerciseId))
                        .ToList();
                    var existingExerciseIds = existingSyllabusExercises.list.Select(se => se.ExerciseId).ToList();
                    var exercisesToAdd = exerciseIds
                        .Where(e => !existingExerciseIds.Contains(e))
                        .Select(e => new SyllabusExercise
                        {
                            SyllabusId = updateDTO.Id,
                            ExerciseTypeId = exerciseTypeId,
                            ExerciseId = e,
                            CreatedDate = DateTime.Now
                        })
                        .ToList();
                    foreach (var deleteExercise in exercisesToDelete)
                    {
                        await _syllabusExerciseRepository.RemoveAsync(deleteExercise);
                    }
                    foreach (var addExercise in exercisesToAdd)
                    {
                        await _syllabusExerciseRepository.CreateAsync(addExercise);
                    }
                }


                var (syllabusExerciseCount, syllabusExercises) = await _syllabusExerciseRepository.GetAllNotPagingAsync(filter: x => x.SyllabusId == model.Id, includeProperties: "Exercise,ExerciseType", excludeProperties: null);
                model.SyllabusExercises = syllabusExercises;
                model.ExerciseTypes = syllabusExercises
                .GroupBy(se => se.ExerciseTypeId)
                .Select(group => new ExerciseTypeDTO
                {
                    Id = group.First().ExerciseType.Id,
                    ExerciseTypeName = group.First().ExerciseType.ExerciseTypeName,
                    Exercises = group.Select(g => new ExerciseDTO
                    {
                        Id = g.Exercise.Id,
                        ExerciseName = g.Exercise.ExerciseName,
                        Description = g.Exercise.Description
                    }).ToList()
                }).ToList();

                _response.StatusCode = HttpStatusCode.NoContent;
                _response.Result = _mapper.Map<SyllabusDTO>(model);
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating syllabus with id: {id}", id);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> CreateAsync(SyllabusCreateDTO createDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
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
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for CreateAsync. ModelState errors: {Errors}", string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.SYLLABUS) };
                    return BadRequest(_response);
                }

                var (total, list) = await _syllabusRepository.GetAllNotPagingAsync(x => createDTO.AgeFrom >= x.AgeFrom && createDTO.AgeEnd >= x.AgeEnd && x.TutorId == userId && !x.IsDeleted);
                foreach (var item in list)
                {
                    if (item.AgeFrom == createDTO.AgeFrom && item.AgeEnd == createDTO.AgeEnd)
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.AGE) };
                        return BadRequest(_response);
                    }
                }
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


                var (syllabusExerciseCount, syllabusExercises) = await _syllabusExerciseRepository.GetAllNotPagingAsync(filter: x => x.SyllabusId == model.Id, includeProperties: "Exercise,ExerciseType", excludeProperties: null);
                model.SyllabusExercises = syllabusExercises;
                model.ExerciseTypes = syllabusExercises
                .GroupBy(se => se.ExerciseTypeId)
                .Select(group => new ExerciseTypeDTO
                {
                    Id = group.First().ExerciseType.Id,
                    ExerciseTypeName = group.First().ExerciseType.ExerciseTypeName,
                    Exercises = group.Select(g => new ExerciseDTO
                    {
                        Id = g.Exercise.Id,
                        ExerciseName = g.Exercise.ExerciseName,
                        Description = g.Exercise.Description
                    }).ToList()
                }).ToList();

                _response.StatusCode = HttpStatusCode.Created;
                _response.Result = _mapper.Map<SyllabusDTO>(model);
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the syllabus.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

    }
}
