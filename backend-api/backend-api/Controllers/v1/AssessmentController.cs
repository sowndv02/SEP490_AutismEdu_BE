using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Repository.IRepository;
using backend_api.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace backend_api.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class AssessmentController : ControllerBase
    {
        private readonly IAssessmentQuestionRepository _assessmentQuestionRepository;
        private readonly IAssessmentScoreRangeRepository _assessmentScoreRangeRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        protected ILogger<AssessmentController> _logger;
        private readonly IResourceService _resourceService;

        public AssessmentController(IAssessmentQuestionRepository assessmentQuestionRepository,
            IMapper mapper, ILogger<AssessmentController> logger,
            IResourceService resourceService, IAssessmentScoreRangeRepository assessmentScoreRangeRepository)
        {
            _resourceService = resourceService;
            _logger = logger;
            _assessmentQuestionRepository = assessmentQuestionRepository;
            _response = new APIResponse();
            _mapper = mapper;
            _assessmentScoreRangeRepository = assessmentScoreRangeRepository;
        }


        [HttpPost]
        [Authorize(Roles = SD.STAFF_ROLE)]
        public async Task<ActionResult<APIResponse>> CreateAsync([FromBody] AssessmentQuestionCreateDTO assessmentQuestionCreateDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (assessmentQuestionCreateDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _logger.LogWarning("Attempted to create an assessment question with a null request payload.");
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ASSESSMENT_QUESTION) };
                    return BadRequest(_response);
                }

                var assessmentExist = await _assessmentQuestionRepository.GetAsync(x => x.Question.Equals(assessmentQuestionCreateDTO.Question));
                if (assessmentExist != null)
                {
                    _logger.LogWarning($"Duplicate question attempted: {assessmentQuestionCreateDTO.Question}");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, assessmentQuestionCreateDTO.Question) };
                    return BadRequest(_response);
                }
                AssessmentQuestion model = _mapper.Map<AssessmentQuestion>(assessmentQuestionCreateDTO);
                model.SubmitterId = userId;
                //model.IsAssessment = true;
                model.IsHidden = false;
                model.CreatedDate = DateTime.Now;
                var assessmentQuestion = await _assessmentQuestionRepository.CreateAsync(model);
                _response.IsSuccess = true;
                _response.StatusCode = HttpStatusCode.Created;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _logger.LogError("Error occurred while creating an assessment question: {Message}", ex.Message);
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllAsync()
        {
            try
            {
                var question = await _assessmentQuestionRepository.GetAllNotPagingAsync(x => x.TestId == null, "AssessmentOptions", null);
                var scoreRange = await _assessmentScoreRangeRepository.GetAllNotPagingAsync();
                AllAssessmentDTO model = new AllAssessmentDTO();
                model.Questions = _mapper.Map<List<AssessmentQuestionDTO>>(question.list);
                model.ScoreRanges = _mapper.Map<List<AssessmentScoreRangeDTO>>(scoreRange.list);
                _response.Result = model;
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _logger.LogError("Error occurred while creating an assessment question: {Message}", ex.Message);
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
