using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Models.DTOs;
using AutismEduConnectSystem.Models.DTOs.CreateDTOs;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.Utils;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using static AutismEduConnectSystem.SD;

namespace AutismEduConnectSystem.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class ExerciseTypeController : ControllerBase
    {
        private readonly IExerciseRepository _exerciseRepository;
        private readonly IExerciseTypeRepository _exerciseTypeRepository;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        protected int pageSize = 0;
        private readonly IResourceService _resourceService;
        private readonly ILogger<ExerciseTypeController> _logger;

        public ExerciseTypeController(IExerciseRepository exerciseRepository, IExerciseTypeRepository exerciseTypeRepository,
            IConfiguration configuration, IMapper mapper, IResourceService resourceService, ILogger<ExerciseTypeController> logger)
        {
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _response = new APIResponse();
            _mapper = mapper;
            _exerciseRepository = exerciseRepository;
            _exerciseTypeRepository = exerciseTypeRepository;
            _resourceService = resourceService;
            _logger = logger;
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse>> GetById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid ExerciseTypeId: {ExerciseTypeId}. Must be greater than zero.", id);
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.EXERCISE_TYPE) };
                    return StatusCode((int)HttpStatusCode.InternalServerError, _response);
                }

                var exercise = await _exerciseTypeRepository.GetAsync(x => x.Id == id, false, null, null);
                if (exercise == null)
                {
                    _logger.LogWarning("ExerciseType with ID {ExerciseTypeId} not found", id);
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.EXERCISE_TYPE) };
                    return NotFound(_response);
                }
                _response.Result = _mapper.Map<ExerciseTypeDTO>(exercise);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving ExerciseType with ID {ExerciseTypeId}", id);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet("getAllNoPaging")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllExerciseTypesAsync([FromQuery] string? search, string? orderBy = SD.CREATED_DATE, string? sort = SD.ORDER_DESC)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                int totalCount = 0;
                List<ExerciseType> list = new();
                Expression<Func<ExerciseType, bool>> filter = e => true;
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                Expression<Func<ExerciseType, object>> orderByQuery = u => true;
                if (userRoles.Contains(SD.TUTOR_ROLE))
                {
                    filter = filter.AndAlso(e => e.IsHide);
                }
                if (!string.IsNullOrEmpty(search))
                {
                    filter = filter.AndAlso(e => e.ExerciseTypeName.Contains(search));
                }
                bool isDesc = !string.IsNullOrEmpty(sort) && sort == SD.ORDER_DESC;

                if (orderBy != null)
                {
                    switch (orderBy)
                    {
                        case SD.CREATED_DATE:
                            orderByQuery = x => x.CreatedDate;
                            break;
                        default:
                            orderByQuery = x => x.CreatedDate;
                            break;
                    }
                }
                var (count, result) = await _exerciseTypeRepository.GetAllNotPagingAsync(filter, includeProperties: null, orderBy: orderByQuery, isDesc: isDesc);
                list = result;
                totalCount = count;
                _response.Result = _mapper.Map<List<ExerciseTypeDTO>>(list);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = null;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving ExerciseTypes with search: {Search}, OrderBy: {OrderBy}, Sort: {Sort}", search, orderBy, sort);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllExerciseTypesAsync([FromQuery] string? search, int pageNumber = 1)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                int totalCount = 0;
                List<ExerciseType> list = new();
                Expression<Func<ExerciseType, bool>> filter = e => true;
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

                if (userRoles.Contains(SD.TUTOR_ROLE))
                {
                    filter = filter.AndAlso(e => e.IsHide);
                }
                if (!string.IsNullOrEmpty(search))
                {
                    filter = filter.AndAlso(e => e.ExerciseTypeName.Contains(search));
                }
                var (count, result) = await _exerciseTypeRepository.GetAllAsync(filter, pageSize: 9, pageNumber: pageNumber);
                list = result;
                totalCount = count;

                Pagination pagination = new() { PageNumber = pageNumber, PageSize = 9, Total = totalCount };

                _response.Result = _mapper.Map<List<ExerciseTypeDTO>>(list);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving ExerciseTypes. Search: {Search}, PageNumber: {PageNumber}", search, pageNumber);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpGet("exercise/{id}")]
        public async Task<ActionResult<APIResponse>> GetExercisesByTypeAsync([FromRoute] int id, [FromQuery] string? search, int pageNumber = 1, string? orderBy = SD.CREATED_DATE, string? sort = SD.ORDER_DESC)
        {
            try
            {
                int totalCount = 0;
                List<Exercise> list = new();
                Expression<Func<Exercise, bool>> filter = e => e.ExerciseTypeId == id;
                Expression<Func<Exercise, object>> orderByQuery = u => true;
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                if (userRoles.Contains(SD.TUTOR_ROLE))
                {
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId))
                    {
                        _logger.LogWarning("Unauthorized access attempt detected.");
                        _response.IsSuccess = false;
                        _response.StatusCode = HttpStatusCode.Unauthorized;
                        _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                        return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                    }
                    filter = filter.AndAlso(e => !e.IsDeleted && e.IsActive && e.TutorId == userId);
                }
                if (!string.IsNullOrEmpty(search))
                {
                    filter = filter.AndAlso(e => e.ExerciseName.Contains(search));
                }

                bool isDesc = !string.IsNullOrEmpty(sort) && sort == SD.ORDER_DESC;

                if (orderBy != null)
                {
                    switch (orderBy)
                    {
                        case SD.CREATED_DATE:
                            orderByQuery = x => x.CreatedDate;
                            break;
                        default:
                            orderByQuery = x => x.CreatedDate;
                            break;
                    }
                }

                var (count, result) = await _exerciseRepository.GetAllAsync(filter: filter, includeProperties: null, pageSize: pageSize, pageNumber: pageNumber, orderBy: orderByQuery, isDesc: isDesc);

                list = result;
                totalCount = count;

                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = totalCount };

                _response.Result = _mapper.Map<List<ExerciseDTO>>(list);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving Exercises for ExerciseTypeId: {Id}. Search: {Search}, PageNumber: {PageNumber}, OrderBy: {OrderBy}, Sort: {Sort}",
                    id, search, pageNumber, orderBy, sort);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost]
        [Authorize(Roles = $"{SD.STAFF_ROLE},{SD.MANAGER_ROLE}")]
        public async Task<IActionResult> CreateExerciseTypeAsync(ExerciseTypeCreateDTO exerciseTypeCreateDTO)
        {
            try
            {
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                if (userRoles == null || (!userRoles.Contains(SD.STAFF_ROLE) && !userRoles.Contains(SD.MANAGER_ROLE)))
                {
                    _logger.LogWarning("Forbidden access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for ExerciseTypeCreateDTO. Returning BadRequest.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.EXERCISE_TYPE) };
                    return BadRequest(_response);
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                var newExerciseType = _mapper.Map<ExerciseType>(exerciseTypeCreateDTO);
                newExerciseType.SubmitterId = userId;
                await _exerciseTypeRepository.CreateAsync(newExerciseType);
                _response.StatusCode = HttpStatusCode.Created;
                _response.Result = _mapper.Map<ExerciseTypeDTO>(newExerciseType);
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating ExerciseType with ExerciseTypeCreateDTO: {@ExerciseTypeCreateDTO}", exerciseTypeCreateDTO);
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPut("changeStatus/{id}")]
        [Authorize(Roles = $"{SD.STAFF_ROLE},{SD.MANAGER_ROLE}")]
        public async Task<IActionResult> UpdateStatusRequest(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                if (userRoles == null || (!userRoles.Contains(SD.STAFF_ROLE) && !userRoles.Contains(SD.MANAGER_ROLE)))
                {
                    _logger.LogWarning("Forbidden access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                if(id <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                ExerciseType exerciseType = await _exerciseTypeRepository.GetAsync(x => x.Id == id, true, "Submitter", null);
                if (exerciseType == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.EXERCISE_TYPE) };
                    return NotFound(_response);
                }
                if (!exerciseType.IsHide)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.EXERCISE_TYPE) };
                    return BadRequest(_response);
                }

                exerciseType.IsHide = false;

                await _exerciseTypeRepository.UpdateAsync(exerciseType);
                _response.Result = _mapper.Map<ExerciseTypeDTO>(exerciseType);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

    }
}