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

namespace AutismEduConnectSystem.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class ExerciseController : ControllerBase
    {
        private readonly IExerciseRepository _exerciseRepository;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        private readonly ILogger<ExerciseController> _logger;
        protected int pageSize = 0;
        private readonly IResourceService _resourceService;


        public ExerciseController(IExerciseRepository exerciseRepository, IConfiguration configuration, IMapper mapper
            , IResourceService resourceService, ILogger<ExerciseController> logger)
        {
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _response = new APIResponse();
            _mapper = mapper;
            _exerciseRepository = exerciseRepository;
            _resourceService = resourceService;
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse>> GetExercisesByTypeAsync([FromRoute] int id, [FromQuery] string? search, string? orderBy = SD.CREATED_DATE, string? sort = SD.ORDER_DESC)
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

                var (count, result) = await _exerciseRepository.GetAllNotPagingAsync(filter: filter, includeProperties: null, orderBy: orderByQuery, isDesc: isDesc);

                list = result;
                totalCount = count;


                _response.Result = _mapper.Map<List<ExerciseDTO>>(list);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = null;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching exercises for ExerciseTypeId: {ExerciseTypeId}, Search: {Search}", id, search);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> CreateExerciseAsync(ExerciseCreateDTO exerciseCreateDTO)
        {
            try
            {
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                if (userRoles == null || (!userRoles.Contains(SD.TUTOR_ROLE)))
                {
                    _logger.LogWarning("Forbidden access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
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
                if (exerciseCreateDTO == null)
                {
                    _logger.LogWarning("Received null ExerciseCreateDTO from user: {UserId}", userId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.EXERCISE) };
                    return BadRequest(_response);
                }

                var exerciseModel = _mapper.Map<Exercise>(exerciseCreateDTO);
                if (exerciseCreateDTO.OriginalId == null || exerciseCreateDTO.OriginalId == 0)
                {
                    exerciseModel.OriginalId = null;
                }
                exerciseModel.VersionNumber = await _exerciseRepository.GetNextVersionNumberAsync(exerciseCreateDTO.OriginalId);
                await _exerciseRepository.DeactivatePreviousVersionsAsync(exerciseCreateDTO.OriginalId);
                exerciseModel.TutorId = userId;
                var createdExercise = await _exerciseRepository.CreateAsync(exerciseModel);

                _response.Result = _mapper.Map<ExerciseDTO>(exerciseModel);
                _response.StatusCode = HttpStatusCode.Created;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating exercise for user: {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        //[HttpPut("changeStatus/{id}")]
        //public async Task<IActionResult> ApproveOrRejectExerciseCreateRequest(ChangeStatusDTO changeStatusDTO)
        //{
        //    try
        //    {
        //        ExerciseType exerciseType = await _exerciseTypeRepository.GetAsync(x => x.Id == changeStatusDTO.Id, false, null, null);

        //        if (exerciseType == null || exerciseType.RequestStatus != Status.PENDING)
        //        {
        //            _response.StatusCode = HttpStatusCode.BadRequest;
        //            _response.IsSuccess = false;
        //            _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
        //            return BadRequest(_response);
        //        }

        //        if (changeStatusDTO.StatusChange == (int)Status.APPROVE)
        //        {
        //            exerciseType.RequestStatus = Status.APPROVE;
        //            await _exerciseTypeRepository.UpdateAsync(exerciseType);

        //            _response.Result = _mapper.Map<ExerciseTypeDTO>(exerciseType);
        //            _response.StatusCode = HttpStatusCode.OK;
        //            _response.IsSuccess = true;
        //            return Ok(_response);
        //        }
        //        else if (changeStatusDTO.StatusChange == (int)Status.REJECT)
        //        {
        //            exerciseType.RequestStatus = Status.REJECT;

        //            await _exerciseTypeRepository.UpdateAsync(exerciseType);

        //            _response.Result = _mapper.Map<ExerciseTypeDTO>(exerciseType);
        //            _response.StatusCode = HttpStatusCode.OK;
        //            _response.IsSuccess = true;
        //            return Ok(_response);
        //        }

        //        _response.StatusCode = HttpStatusCode.NoContent;
        //        _response.IsSuccess = true;
        //        return Ok(_response);
        //    }
        //    catch (Exception ex)
        //    {
        //        _response.IsSuccess = false;
        //        _response.StatusCode = HttpStatusCode.InternalServerError;
        //        _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
        //        return StatusCode((int)HttpStatusCode.InternalServerError, _response);
        //    }
        //}

        [HttpDelete("{id}")]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<IActionResult> DeleteExerciseAsync(int id)
        {
            try
            {
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                if (userRoles == null || (!userRoles.Contains(SD.TUTOR_ROLE)))
                {
                    _logger.LogWarning("Forbidden access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
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
                var exercise = await _exerciseRepository.GetAsync(x => x.Id == id && !x.IsDeleted && x.IsActive && x.TutorId == userId, false, null);
                if (exercise == null)
                {
                    _logger.LogWarning("Exercise with ID {ExerciseId} not found for user: {UserId}", id, userId);
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.EXERCISE) };
                    return NotFound(_response);
                }

                exercise.IsDeleted = true;
                var model = await _exerciseRepository.UpdateAsync(exercise);
                _response.Result = _mapper.Map<ExerciseDTO>(model);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting exercise with ID {ExerciseId} for user: {UserId}", id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

    }
}