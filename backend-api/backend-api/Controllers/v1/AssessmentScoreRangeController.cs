using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models.DTOs.UpdateDTOs;
using backend_api.Repository;
using backend_api.Repository.IRepository;
using backend_api.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace backend_api.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class AssessmentScoreRangeController : ControllerBase
    {
        private readonly IAssessmentScoreRangeRepository _assessmentScoreRangeRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        protected ILogger<AssessmentScoreRangeController> _logger;
        private readonly IResourceService _resourceService;

        public AssessmentScoreRangeController(IAssessmentScoreRangeRepository assessmentScoreRangeRepository, 
            IMapper mapper, ILogger<AssessmentScoreRangeController> logger, IResourceService resourceService)
        {
            _assessmentScoreRangeRepository = assessmentScoreRangeRepository;
            _mapper = mapper;
            _logger = logger;
            _resourceService = resourceService;
            _response = new APIResponse();
        }

        [HttpPost]
        [Authorize(Roles = $"{SD.STAFF_ROLE},{SD.MANAGER_ROLE}")]
        public async Task<ActionResult<APIResponse>> CreateAsync(AssessmentScoreRangeCreateDTO createDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (createDTO == null)
                {
                    _logger.LogWarning("Received null createDTO for AssessmentScoreRange creation.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ASSESSMENT_QUESTION) };
                    return BadRequest(_response);
                }

                if (createDTO.MinScore > createDTO.MaxScore)
                {
                    _logger.LogWarning("Invalid score range: MinScore ({MinScore}) is greater than MaxScore ({MaxScore}).", createDTO.MinScore, createDTO.MaxScore);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.SCORE_RANGE) };
                    return BadRequest(_response);
                }

                //var rangeOverLap = await _assessmentScoreRangeRepository.GetAsync(x => createDTO.MinScore <= x.MaxScore && createDTO.MaxScore >= x.MinScore);
                //if(rangeOverLap != null)
                //{
                //    _logger.LogWarning("Overlap found with existing score range: MinScore = {MinScore}, MaxScore = {MaxScore}",
                //     rangeOverLap.MinScore, rangeOverLap.MaxScore);
                //    _response.StatusCode = HttpStatusCode.BadRequest;
                //    _response.IsSuccess = false;
                //    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.ASSESSMENT_SCORE_RANGE_DUPLICATED_MESSAGE, rangeOverLap.MinScore, rangeOverLap.MaxScore) };
                //    return BadRequest(_response);
                //}
                var model = _mapper.Map<AssessmentScoreRange>(createDTO);
                model.CreateBy = userId;
                model.CreateDate = DateTime.Now;
                model = await _assessmentScoreRangeRepository.CreateAsync(model);

                _response.Result = _mapper.Map<AssessmentScoreRangeDTO>(model);
                _response.IsSuccess = true;
                _response.StatusCode = HttpStatusCode.Created;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating AssessmentScoreRange with MinScore: {MinScore} and MaxScore: {MaxScore}",
                    createDTO?.MinScore, createDTO?.MaxScore);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllAsync()
        {
            try
            {
                var result = await _assessmentScoreRangeRepository.GetAllNotPagingAsync();
                _response.Result = _mapper.Map<List<AssessmentScoreRangeDTO>>(result.list);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all assessment score ranges.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPut]
        [Authorize(Roles = $"{SD.STAFF_ROLE},{SD.MANAGER_ROLE}")]
        public async Task<ActionResult<APIResponse>> UpdateAsync(AssessmentScoreRangeUpdateDTO updateDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (updateDTO == null || updateDTO.Id <= 0)
                {
                    _logger.LogWarning("Invalid update request for AssessmentScoreRange with ID: {AssessmentScoreRangeId}", updateDTO?.Id);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ASSESSMENT_SCORE_RANGE) };
                    return BadRequest(_response);
                }

                var model = await _assessmentScoreRangeRepository.GetAsync(x => x.Id == updateDTO.Id);

                if (model == null)
                {
                    _logger.LogWarning("Assessment score range with ID: {AssessmentScoreRangeId} not found for update.", updateDTO?.Id);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.ASSESSMENT_SCORE_RANGE) };
                    return BadRequest(_response);
                }


                if (updateDTO.MinScore > updateDTO.MaxScore)
                {
                    _logger.LogWarning("Invalid score range. MinScore: {MinScore} is greater than MaxScore: {MaxScore}", updateDTO.MinScore, updateDTO.MaxScore);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.SCORE_RANGE) };
                    return BadRequest(_response);
                }

                //var rangeOverLap = await _assessmentScoreRangeRepository.GetAsync(x => updateDTO.MinScore <= x.MaxScore && updateDTO.MaxScore >= x.MinScore && x != model);
                //if (rangeOverLap != null)
                //{
                //    _logger.LogWarning("Duplicate score range found with MinScore: {MinScore} and MaxScore: {MaxScore}", rangeOverLap.MinScore, rangeOverLap.MaxScore);
                //    _response.StatusCode = HttpStatusCode.BadRequest;
                //    _response.IsSuccess = false;
                //    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.ASSESSMENT_SCORE_RANGE_DUPLICATED_MESSAGE, rangeOverLap.MinScore, rangeOverLap.MaxScore) };
                //    return BadRequest(_response);
                //}

                model.Description = updateDTO.Description;
                model.MinScore = updateDTO.MinScore;
                model.MaxScore = updateDTO.MaxScore;
                model.UpdateDate = DateTime.Now;

                model = await _assessmentScoreRangeRepository.UpdateAsync(model);

                _response.Result = _mapper.Map<AssessmentScoreRangeDTO>(model);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating assessment score range with ID: {AssessmentScoreRangeId}", updateDTO?.Id);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = $"{SD.STAFF_ROLE},{SD.MANAGER_ROLE}")]
        public async Task<ActionResult<APIResponse>> DeleteAsync(int id)
        {
            try
            {
                if(id <= 0)
                {
                    _logger.LogWarning("Invalid delete request for AssessmentScoreRange with ID: {AssessmentScoreRangeId}", id);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }

                var model = await _assessmentScoreRangeRepository.GetAsync(x => x.Id == id);

                if (model == null)
                {
                    _logger.LogWarning("Assessment score range with ID: {AssessmentScoreRangeId} not found for deletion.", id);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.ASSESSMENT_SCORE_RANGE) };
                    return BadRequest(_response);
                }

                await _assessmentScoreRangeRepository.RemoveAsync(model);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting assessment score range with ID: {AssessmentScoreRangeId}", id);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
