using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models.DTOs.UpdateDTOs;
using backend_api.Repository.IRepository;
using backend_api.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using static backend_api.SD;

namespace backend_api.Controllers.v1
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

        public ExerciseTypeController(IExerciseRepository exerciseRepository, IExerciseTypeRepository exerciseTypeRepository, IConfiguration configuration, IMapper mapper)
        {
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _response = new APIResponse();
            _mapper = mapper;
            _exerciseRepository = exerciseRepository;
            _exerciseTypeRepository = exerciseTypeRepository;
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllExerciseTypesAsync([FromQuery] string? search, string? status = SD.STATUS_ALL, int pageNumber = 1, int pageSize = 9)
        {
            try
            {
                int totalCount = 0;
                List<ExerciseType> list = new();
                //Expression<Func<ExerciseType, bool>> filter = e => true;
                Expression<Func<ExerciseType, bool>> filter = e => !e.IsDeleted;

                if (!string.IsNullOrEmpty(search))
                {
                    filter = filter.AndAlso(e => e.ExerciseTypeName.Contains(search));
                }
                if (!string.IsNullOrEmpty(status) && status != SD.STATUS_ALL)
                {
                    switch (status.ToLower())
                    {
                        case "approve":
                            filter = filter.AndAlso(e => e.RequestStatus == Status.APPROVE);
                            break;
                        case "reject":
                            filter = filter.AndAlso(e => e.RequestStatus == Status.REJECT);
                            break;
                        case "pending":
                            filter = filter.AndAlso(e => e.RequestStatus == Status.PENDING);
                            break;
                    }
                }

                var (count, result) = await _exerciseTypeRepository.GetAllAsync(filter, pageSize: pageSize, pageNumber: pageNumber);
                list = result;
                totalCount = count;

                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = totalCount };

                _response.Result = _mapper.Map<List<ExerciseTypeDTO>>(list);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;
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

        [HttpGet("tutor")]
        public async Task<ActionResult<APIResponse>> GetAllExerciseTypesByTutorAsync([FromQuery] string? search, string? status = SD.STATUS_ALL, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.NOT_FOUND_MESSAGE };
                    return Unauthorized(_response);
                }

                int totalCount = 0;
                List<ExerciseType> list = new();
                Expression<Func<ExerciseType, bool>> filter = e => e.TutorId == userId;

                if (!string.IsNullOrEmpty(search))
                {
                    filter = filter.AndAlso(e => e.ExerciseTypeName.Contains(search));
                }

                if (!string.IsNullOrEmpty(status) && status != SD.STATUS_ALL)
                {
                    switch (status.ToLower())
                    {
                        case "approve":
                            filter = filter.AndAlso(e => e.RequestStatus == Status.APPROVE);
                            break;
                        case "reject":
                            filter = filter.AndAlso(e => e.RequestStatus == Status.REJECT);
                            break;
                        case "pending":
                            filter = filter.AndAlso(e => e.RequestStatus == Status.PENDING);
                            break;
                    }
                }

                var (count, result) = await _exerciseTypeRepository.GetAllAsync(filter, pageSize: pageSize, pageNumber: pageNumber);
                list = result;
                totalCount = count;

                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = totalCount };

                _response.Result = _mapper.Map<List<ExerciseTypeDTO>>(list);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;
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


        [HttpGet("{exerciseTypeId}")]
        public async Task<ActionResult<APIResponse>> GetExercisesByTypeAsync(int exerciseTypeId, [FromQuery] string? search, int pageNumber = 1, int pageSize = 10, string? orderBy = SD.CREADTED_DATE, string? sort = SD.ORDER_DESC)
        {
            try
            {
                int totalCount = 0;
                List<Exercise> list = new();
                Expression<Func<Exercise, bool>> filter = e => e.ExerciseTypeId == exerciseTypeId;
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
                        case SD.CREADTED_DATE:
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
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost]
        public async Task<ActionResult<APIResponse>> CreateExerciseAsync(ExerciseCreateDTO exerciseCreateDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                //var userId = "78123797-c58d-4d92-8671-8d4b1f6bd4a9";

                if (exerciseCreateDTO == null || string.IsNullOrEmpty(userId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }

                var exerciseModel = _mapper.Map<Exercise>(exerciseCreateDTO);
                exerciseModel.TutorId = userId;
                exerciseModel.CreatedDate = DateTime.Now;

                var createdExercise = await _exerciseRepository.CreateAsync(exerciseModel);

                var exerciseDTO = new ExerciseDTO
                {
                    Id = createdExercise.ExerciseId,
                    ExerciseName = createdExercise.ExerciseName,
                    ExerciseContent = createdExercise.ExerciseContent,
                    TutorId = createdExercise.TutorId,
                    ExerciseTypeId = createdExercise.ExerciseTypeId
                };

                _response.Result = exerciseDTO;
                _response.StatusCode = HttpStatusCode.Created;
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

        [HttpPost("ExerciseTypeCreate")]
        public async Task<IActionResult> CreateExerciseTypeAsync(ExerciseTypeCreateDTO exerciseTypeCreateDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                //var userId = "78123797-c58d-4d92-8671-8d4b1f6bd4a9";

                var newExerciseType = _mapper.Map<ExerciseType>(exerciseTypeCreateDTO);
                newExerciseType.TutorId = userId;
                newExerciseType.RequestStatus = Status.PENDING;

                await _exerciseTypeRepository.CreateAsync(newExerciseType);

                _response.StatusCode = HttpStatusCode.Created;
                _response.Result = _mapper.Map<ExerciseTypeDTO>(newExerciseType);
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPut("changeStatus/{id}")]
        public async Task<IActionResult> ApproveOrRejectExerciseCreateRequest(ChangeStatusDTO changeStatusDTO)
        {
            try
            {
                ExerciseType exerciseType = await _exerciseTypeRepository.GetAsync(x => x.Id == changeStatusDTO.Id, false, null, null);
                if (exerciseType == null || exerciseType.RequestStatus != Status.PENDING)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }

                if (changeStatusDTO.StatusChange == (int)Status.APPROVE)
                {
                    exerciseType.RequestStatus = Status.APPROVE;

                    await _exerciseTypeRepository.UpdateAsync(exerciseType);

                    _response.Result = _mapper.Map<ExerciseTypeDTO>(exerciseType);
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    return Ok(_response);
                }
                else if (changeStatusDTO.StatusChange == (int)Status.REJECT)
                {
                    exerciseType.RequestStatus = Status.REJECT;

                    await _exerciseTypeRepository.UpdateAsync(exerciseType);

                    _response.Result = _mapper.Map<ExerciseTypeDTO>(exerciseType);
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    return Ok(_response);
                }

                _response.StatusCode = HttpStatusCode.NoContent;
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

        [HttpPut("{exerciseId}")]
        public async Task<IActionResult> UpdateExercise(int exerciseId, ExerciseUpdateDTO exerciseUpdateDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                //var userId = "78123797-c58d-4d92-8671-8d4b1f6bd4a9";

                var existingExercise = await _exerciseRepository.GetAsync(e => e.ExerciseId == exerciseId);

                if (existingExercise == null || existingExercise.TutorId != userId)
                {
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { "Unauthorized to update this exercise." };
                    return Forbid();
                }

                existingExercise.ExerciseName = exerciseUpdateDTO.ExerciseName;
                existingExercise.ExerciseContent = exerciseUpdateDTO.ExerciseContent;
                existingExercise.UpdatedDate = DateTime.UtcNow;

                await _exerciseRepository.UpdateAsync(existingExercise);

                _response.Result = _mapper.Map<ExerciseDTO>(existingExercise);
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

        public async Task<IActionResult> DeleteExerciseAsync(int id)
        {
            try
            {
                var exercise = await _exerciseRepository.GetAsync(x => x.ExerciseId == id && !x.IsDeleted);
                if (exercise == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.NOT_FOUND_MESSAGE };
                    return NotFound(_response);
                }

                exercise.IsDeleted = true;
                await _exerciseRepository.UpdateAsync(exercise);

                _response.StatusCode = HttpStatusCode.NoContent;
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

        [HttpDelete("{exerciseTypeId}")]
        public async Task<IActionResult> DeleteExerciseTypeByIdAsync(int id)
        {
            try
            {
                var exerciseType = await _exerciseTypeRepository.GetAsync(x => x.Id == id && !x.IsDeleted);
                if (exerciseType == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.NOT_FOUND_MESSAGE };
                    return NotFound(_response);
                }

                exerciseType.IsDeleted = true;
                await _exerciseTypeRepository.UpdateAsync(exerciseType);

                _response.StatusCode = HttpStatusCode.NoContent;
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