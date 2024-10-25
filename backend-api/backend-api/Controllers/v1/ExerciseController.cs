using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models.DTOs.UpdateDTOs;
using backend_api.Repository;
using backend_api.Repository.IRepository;
using backend_api.Utils;
using Microsoft.AspNetCore.Authorization;
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
    public class ExerciseController : ControllerBase
    {
        private readonly IExerciseRepository _exerciseRepository;
        private readonly IExerciseTypeRepository _exerciseTypeRepository;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        protected int pageSize = 0;

        public ExerciseController(IExerciseRepository exerciseRepository, IExerciseTypeRepository exerciseTypeRepository, IConfiguration configuration, IMapper mapper)
        {
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _response = new APIResponse();
            _mapper = mapper;
            _exerciseRepository = exerciseRepository;
            _exerciseTypeRepository = exerciseTypeRepository;
        }

        // Get by Id


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
                if (exerciseCreateDTO.OriginalId == null || exerciseCreateDTO.OriginalId == 0)
                {
                    exerciseModel.OriginalId = null;
                }
                exerciseModel.VersionNumber = await _exerciseRepository.GetNextVersionNumberAsync(exerciseCreateDTO.OriginalId);
                await _exerciseRepository.DeactivatePreviousVersionsAsync(exerciseModel.OriginalId);
                exerciseModel.TutorId = userId;
                var createdExercise = await _exerciseRepository.CreateAsync(exerciseModel);

                _response.Result = _mapper.Map<ExerciseDTO>(exerciseModel);
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
        //        _response.ErrorMessages = new List<string> { ex.Message };
        //        return StatusCode((int)HttpStatusCode.InternalServerError, _response);
        //    }
        //}

        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> DeleteExerciseAsync(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var exercise = await _exerciseRepository.GetAsync(x => x.Id == id && !x.IsDeleted && x.IsActive && x.TutorId == userId, false, null);
                if (exercise == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.NOT_FOUND_MESSAGE };
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
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

    }
}