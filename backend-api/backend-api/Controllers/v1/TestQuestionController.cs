using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Repository;
using backend_api.Repository.IRepository;
using backend_api.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;

namespace backend_api.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class TestQuestionController : ControllerBase
    {
        private readonly ITestRepository _testRepository;
        private readonly ITestResultRepository _resultRepository;
        private readonly ITestResultDetailRepository _resultDetailRepository;
        private readonly IAssessmentQuestionRepository _assessmentQuestionRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        protected ILogger<TestQuestionController> _logger;
        private readonly IResourceService _resourceService;

        public TestQuestionController(ITestRepository testRepository, ITestResultRepository resultRepository,
            ITestResultDetailRepository resultDetailRepository, IMapper mapper, ILogger<TestQuestionController> logger,
            IResourceService resourceService, IAssessmentQuestionRepository assessmentQuestionRepository)
        {
            _testRepository = testRepository;
            _resultRepository = resultRepository;
            _resultDetailRepository = resultDetailRepository;
            _assessmentQuestionRepository = assessmentQuestionRepository;
            _mapper = mapper;
            _logger = logger;
            _resourceService = resourceService;
            _response = new APIResponse();
        }

        [HttpPost]
        [Authorize(Roles = SD.STAFF_ROLE)]
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

    }
}
