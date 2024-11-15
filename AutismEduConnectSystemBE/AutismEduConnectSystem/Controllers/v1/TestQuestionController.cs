using AutoMapper;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Models.DTOs;
using AutismEduConnectSystem.Models.DTOs.CreateDTOs;
using AutismEduConnectSystem.Repository;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;

namespace AutismEduConnectSystem.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class TestQuestionController : ControllerBase
    {
        private readonly IAssessmentQuestionRepository _assessmentQuestionRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        protected ILogger<TestQuestionController> _logger;
        private readonly IResourceService _resourceService;

        public TestQuestionController(IMapper mapper, ILogger<TestQuestionController> logger,
            IResourceService resourceService, IAssessmentQuestionRepository assessmentQuestionRepository)
        {
            _assessmentQuestionRepository = assessmentQuestionRepository;
            _mapper = mapper;
            _logger = logger;
            _resourceService = resourceService;
            _response = new APIResponse();
        }

        [HttpPost]
        [Authorize(Roles = $"{SD.STAFF_ROLE},{SD.MANAGER_ROLE}")]
        public async Task<ActionResult<APIResponse>> CreateAsync(TestQuestionCreateDTO createDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (createDTO == null)
                {
                    _logger.LogWarning("CreateAsync received null createDTO");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.QUESTION) };
                    return BadRequest(_response);
                }

                var testExist = await _assessmentQuestionRepository.GetAsync(x => x.TestId == createDTO.TestId && x.Question.Equals(createDTO.Question));
                if (testExist != null)
                {
                    _logger.LogWarning("Duplicate question detected for TestId={TestId}, Question={Question}", createDTO.TestId, createDTO.Question);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.QUESTION) };
                    return BadRequest(_response);
                }

                AssessmentQuestion model = _mapper.Map<AssessmentQuestion>(createDTO);
                if (createDTO.OriginalId == null || createDTO.OriginalId == 0)
                {
                    model.OriginalId = null;
                }
                model.VersionNumber = await _assessmentQuestionRepository.GetNextVersionNumberAsync(createDTO.OriginalId);
                await _assessmentQuestionRepository.DeactivatePreviousVersionsAsync(createDTO.OriginalId);
                model.SubmitterId = userId;
                model.IsHidden = false;
                model.TestId = createDTO.TestId;
                model.CreatedDate = DateTime.Now;

                var testQuestion = await _assessmentQuestionRepository.CreateAsync(model);

                _response.Result = _mapper.Map<AssessmentQuestionDTO>(testQuestion);
                _response.IsSuccess = true;
                _response.StatusCode = HttpStatusCode.Created;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating a test question.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }



        [HttpGet("{testId}")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllAsync(int testId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var model = await _assessmentQuestionRepository.GetAllWithIncludeAsync(x => x.TestId == testId, "AssessmentOptions");

                _response.Result = _mapper.Map<List<AssessmentQuestionDTO>>(model.list);
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

        [HttpDelete("{id}")]
        [Authorize(Roles = $"{SD.STAFF_ROLE},{SD.MANAGER_ROLE}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var question = await _assessmentQuestionRepository.GetAsync(x => x.Id == id 
                                                                                   && x.IsActive 
                                                                                   && !x.IsHidden, true, "AssessmentOptions");
                if (question == null)
                {
                    _logger.LogWarning("Test question with ID {QuestionId} not found for user: {UserId}", id, userId);
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.EXERCISE) };
                    return NotFound(_response);
                }

                question.IsHidden = true;
                var model = await _assessmentQuestionRepository.UpdateAsync(question);
                _response.Result = _mapper.Map<AssessmentQuestionDTO>(model);
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
