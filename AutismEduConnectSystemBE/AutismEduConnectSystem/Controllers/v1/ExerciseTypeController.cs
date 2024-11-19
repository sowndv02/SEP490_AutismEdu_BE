using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Models.DTOs;
using AutismEduConnectSystem.Models.DTOs.CreateDTOs;
using AutismEduConnectSystem.Models.DTOs.UpdateDTOs;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.Utils;
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
    public class ExerciseTypeController : ControllerBase
    {
        private readonly IExerciseRepository _exerciseRepository;
        private readonly IExerciseTypeRepository _exerciseTypeRepository;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        private readonly IResourceService _resourceService;
        private readonly ILogger<ExerciseTypeController> _logger;

        public ExerciseTypeController(IExerciseRepository exerciseRepository, IExerciseTypeRepository exerciseTypeRepository,
            IMapper mapper, IResourceService resourceService, ILogger<ExerciseTypeController> logger)
        {
            _response = new APIResponse();
            _mapper = mapper;
            _exerciseRepository = exerciseRepository;
            _exerciseTypeRepository = exerciseTypeRepository;
            _resourceService = resourceService;
            _logger = logger;
        }


        [HttpPut("{exerciseTypeId}")]
        [Authorize(Roles = $"${SD.STAFF_ROLE},{SD.MANAGER_ROLE}")]
        public async Task<ActionResult<APIResponse>> UpdateAsync(int exerciseTypeId, ExerciseTypeUpdateDTO updateDTO)
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
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                if (!ModelState.IsValid || exerciseTypeId != updateDTO.Id)
                {
                    _logger.LogWarning("Invalid model state for ExerciseTypeCreateDTO. Returning BadRequest.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.EXERCISE_TYPE) };
                    return BadRequest(_response);
                }

                var model = await _exerciseTypeRepository.GetAsync(x => x.Id == updateDTO.Id, true, null, null);
                if (model == null)
                {
                    _logger.LogWarning("Exercise Type not found");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.EXERCISE_TYPE) };
                    return StatusCode((int)HttpStatusCode.NotFound, _response);
                }

                var isExist = await _exerciseTypeRepository.GetAsync(x => x.ExerciseTypeName.ToLower().Equals(updateDTO.ExerciseTypeName.ToLower()) && x.Id != updateDTO.Id, false, null, null);
                if (isExist != null)
                {
                    _logger.LogWarning("ExerciseTypeName already exists");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.EXERCISE_TYPE) };
                    return BadRequest(_response);
                }

                model.ExerciseTypeName = updateDTO.ExerciseTypeName;
                model.UpdatedDate = DateTime.Now;
                var exerciseType = await _exerciseTypeRepository.UpdateAsync(model);
                _response.Result = _mapper.Map<ExerciseTypeDTO>(exerciseType);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating Child information for ParentId: {ParentId}, ChildId: {ChildId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, updateDTO.Id);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet]
        [Authorize(Roles = $"{SD.TUTOR_ROLE},{SD.STAFF_ROLE},{SD.MANAGER_ROLE}")]
        public async Task<ActionResult<APIResponse>> GetAllExerciseTypesAsync([FromQuery] string? search, string? isHide = SD.STATUS_ALL, string? orderBy = SD.CREATED_DATE, string? sort = SD.ORDER_DESC, int pageSize = 0, int pageNumber = 1)
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
                if (userRoles == null || (!userRoles.Contains(SD.TUTOR_ROLE) && !userRoles.Contains(SD.STAFF_ROLE) && !userRoles.Contains(SD.MANAGER_ROLE)))
                {
                    _logger.LogWarning("Forbidden access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }

                int totalCount = 0;
                List<ExerciseType> list = new();
                Expression<Func<ExerciseType, bool>> filter = e => true;
                Expression<Func<ExerciseType, object>> orderByQuery = x => true;

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
                else
                {
                    orderByQuery = x => x.CreatedDate;
                }

                if (userRoles.Contains(SD.TUTOR_ROLE))
                {
                    filter = filter.AndAlso(e => !e.IsHide);
                }

                if (!string.IsNullOrEmpty(isHide) && isHide != SD.STATUS_ALL)
                {
                    switch (isHide.ToLower())
                    {
                        case "true":
                            filter = filter.AndAlso(x => x.IsHide);
                            break;
                        case "false":
                            filter = filter.AndAlso(x => !x.IsHide);
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(search))
                {
                    filter = filter.AndAlso(e => e.ExerciseTypeName.Contains(search));
                }
                bool isDesc = !string.IsNullOrEmpty(sort) && sort == SD.ORDER_DESC;
                if (pageSize != 0)
                {
                    var (countPaging, resultPaging) = await _exerciseTypeRepository.GetAllAsync(filter, null, pageSize: pageSize, pageNumber: pageNumber, orderByQuery, isDesc);
                    list = resultPaging;
                    totalCount = countPaging;
                }
                else if (pageSize == 0)
                {
                    var (count, result) = await _exerciseTypeRepository.GetAllNotPagingAsync(filter, includeProperties: null, null, orderBy: orderByQuery, isDesc: isDesc);
                    list = result;
                    totalCount = count;
                }


                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = totalCount };
                var responseResult = _mapper.Map<List<ExerciseTypeDTO>>(list);
                _response.Result = responseResult;
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
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> GetExercisesByTypeAsync([FromRoute] int id, [FromQuery] string? search, int pageNumber = 1, string? orderBy = SD.CREATED_DATE, string? sort = SD.ORDER_DESC)
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
                if (userRoles == null || (!userRoles.Contains(SD.TUTOR_ROLE)))
                {
                    _logger.LogWarning("Forbidden access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid Exercise ID: {Id}. Returning BadRequest.", id);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                Expression<Func<Exercise, bool>> filter = e => e.ExerciseTypeId == id && !e.IsDeleted && e.IsActive && e.TutorId == userId;
                Expression<Func<Exercise, object>> orderByQuery = u => true;
                
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
                else
                {
                    orderByQuery = x => x.CreatedDate;
                }

                var (count, result) = await _exerciseRepository.GetAllAsync(filter: filter, includeProperties: null, pageSize: 10, pageNumber: pageNumber, orderBy: orderByQuery, isDesc: isDesc);
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = 10, Total = count };
                _response.Result = _mapper.Map<List<ExerciseDTO>>(result);
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
        public async Task<ActionResult<APIResponse>> CreateAsync(ExerciseTypeCreateDTO exerciseTypeCreateDTO)
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
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for ExerciseTypeCreateDTO. Returning BadRequest.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.EXERCISE_TYPE) };
                    return BadRequest(_response);
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
        public async Task<ActionResult<APIResponse>> UpdateStatusRequest(int id)
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
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                if (id <= 0)
                {
                    _logger.LogWarning("id invalid.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                ExerciseType exerciseType = await _exerciseTypeRepository.GetAsync(x => x.Id == id, true, "Submitter", null);
                if (exerciseType == null)
                {
                    _logger.LogWarning("Not found exercise Type with Id: {@id}.", id);
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.EXERCISE_TYPE) };
                    return NotFound(_response);
                }
                if (!exerciseType.IsHide)
                {
                    _logger.LogWarning("exercise Type not hide cannot update hide or show with Id: {@id}.", id);
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
                _logger.LogError(ex, "Error occurred while creating ExerciseType with id: {@id}", id);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

    }
}