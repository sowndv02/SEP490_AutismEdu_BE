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
        protected ILogger<AssessmentScoreRange> _logger;
        private readonly IResourceService _resourceService;

        public AssessmentScoreRangeController(IAssessmentScoreRangeRepository assessmentScoreRangeRepository, 
            IMapper mapper, ILogger<AssessmentScoreRange> logger, IResourceService resourceService)
        {
            _assessmentScoreRangeRepository = assessmentScoreRangeRepository;
            _mapper = mapper;
            _logger = logger;
            _resourceService = resourceService;
            _response = new APIResponse();
        }

        [HttpPost]
        //[Authorize]
        public async Task<ActionResult<APIResponse>> CreateAsync(AssessmentScoreRangeCreateDTO createDTO)
        {
            try
            {
                //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                //if (createDTO == null)
                //{
                //    _response.StatusCode = HttpStatusCode.BadRequest;
                //    _response.IsSuccess = false;
                //    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ASSESSMENT_QUESTION) };
                //    return BadRequest(_response);
                //}

                if (createDTO.MinScore > createDTO.MaxScore)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.SCORE_RANGE) };
                    return BadRequest(_response);
                }

                var rangeOverLap = await _assessmentScoreRangeRepository.GetAsync(x => createDTO.MinScore <= x.MaxScore && createDTO.MaxScore >= x.MinScore);
                if(rangeOverLap != null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.ASSESSMENT_SCORE_RANGE_DUPLICATED_MESSAGE, rangeOverLap.MinScore, rangeOverLap.MaxScore) };
                    return BadRequest(_response);
                }
                // TODO: remove fixed id
                var model = _mapper.Map<AssessmentScoreRange>(createDTO);
                model.CreateBy = "cddcd5ed-a26b-466f-8af6-d2ac4174cd6e";//userId;
                model.CreateDate = DateTime.Now;
                model = await _assessmentScoreRangeRepository.CreateAsync(model);

                _response.Result = _mapper.Map<AssessmentScoreRangeDTO>(model);
                _response.IsSuccess = true;
                _response.StatusCode = HttpStatusCode.Created;
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
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPut]
        //[Authorize]
        public async Task<ActionResult<APIResponse>> UpdateAsync(AssessmentScoreRangeUpdateDTO updateDTO)
        {
            try
            {
                if (updateDTO == null || updateDTO.Id <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ASSESSMENT_SCORE_RANGE) };
                    return BadRequest(_response);
                }

                var model = await _assessmentScoreRangeRepository.GetAsync(x => x.Id == updateDTO.Id);

                if (model == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.ASSESSMENT_SCORE_RANGE) };
                    return BadRequest(_response);
                }


                if (updateDTO.MinScore > updateDTO.MaxScore)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.SCORE_RANGE) };
                    return BadRequest(_response);
                }

                var rangeOverLap = await _assessmentScoreRangeRepository.GetAsync(x => updateDTO.MinScore <= x.MaxScore && updateDTO.MaxScore >= x.MinScore && x != model);
                if (rangeOverLap != null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.ASSESSMENT_SCORE_RANGE_DUPLICATED_MESSAGE, rangeOverLap.MinScore, rangeOverLap.MaxScore) };
                    return BadRequest(_response);
                }

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
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpDelete("{id}")]
        //[Authorize]
        public async Task<ActionResult<APIResponse>> DeleteAsync(int id)
        {
            try
            {
                if(id == null || id <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }

                var model = await _assessmentScoreRangeRepository.GetAsync(x => x.Id == id);

                if (model == null)
                {
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
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
