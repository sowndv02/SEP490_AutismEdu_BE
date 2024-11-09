using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using AutoMapper;
using backend_api.Repository.IRepository;
using backend_api.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using backend_api.Models.DTOs;
using backend_api.Repository;

namespace backend_api.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class TestResultController : ControllerBase
    {
        private readonly ITestResultRepository _testResultRepository;
        private readonly ITestResultDetailRepository _testResultDetailRepository;
        private readonly IAssessmentQuestionRepository _assessmentQuestionRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        protected ILogger<TestController> _logger;
        private readonly IResourceService _resourceService;

        public TestResultController(ITestResultRepository testResultRepository, 
            ITestResultDetailRepository testResultDetailRepository, IMapper mapper, ILogger<TestController> logger, 
            IResourceService resourceService, IAssessmentQuestionRepository assessmentQuestionRepository)
        {
            _testResultRepository = testResultRepository;
            _testResultDetailRepository = testResultDetailRepository;
            _assessmentQuestionRepository = assessmentQuestionRepository;
            _mapper = mapper;
            _logger = logger;
            _resourceService = resourceService;
            _response = new APIResponse();
        }

        [HttpPost("SubmitTest")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> SubmitTest(TestResultCreateDTO createDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (createDTO == null)
                {
                    _logger.LogWarning("Received null TestResultCreateDTO object.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.TEST_RESULT) };
                    return BadRequest(_response);
                }

                var model = _mapper.Map<TestResult>(createDTO);
                model.CreatedDate = DateTime.Now;
                model.ParentId = userId;
                await _testResultRepository.CreateAsync(model);

                _response.IsSuccess = true;
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while submiting tests.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet("{testId}")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllParentTestResult(int testId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                var model = await _testResultRepository.GetAllNotPagingAsync(x => x.TestId == testId && x.ParentId.Equals(userId), "Test");

                _response.Result = _mapper.Map<List<TestResultDTO>>(model.list);
                _response.IsSuccess = true;
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while submiting tests.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet("GetTestResultDetail/{testResultId}")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetTestResultDetail(int testResultId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var model = await _testResultRepository.GetAsync(x => x.Id == testResultId && x.ParentId.Equals(userId), true, "Test");
                var testResult = await _testResultDetailRepository.GetAllNotPagingAsync(x => x.TestResultId == testResultId, "Question,Option");
                var questions = await _assessmentQuestionRepository.GetAllNotPagingAsync(x => x.TestId == model.TestId, "AssessmentOptions");

                model.Results = testResult.list;

                var result = _mapper.Map<TestResultDTO>(model);
                result.TestQuestions = _mapper.Map<List<AssessmentQuestionDTO>>(questions.list);

                _response.Result = result;
                _response.IsSuccess = true;
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while submiting tests.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
