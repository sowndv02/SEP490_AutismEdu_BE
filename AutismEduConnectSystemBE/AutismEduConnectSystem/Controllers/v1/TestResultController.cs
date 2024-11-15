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
    public class TestResultController : ControllerBase
    {
        private readonly ITestResultRepository _testResultRepository;
        private readonly ITestResultDetailRepository _testResultDetailRepository;
        private readonly IAssessmentQuestionRepository _assessmentQuestionRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        protected ILogger<TestController> _logger;
        private readonly IResourceService _resourceService;
        protected int pageSize = 0;

        public TestResultController(ITestResultRepository testResultRepository,
            ITestResultDetailRepository testResultDetailRepository, IMapper mapper, ILogger<TestController> logger,
            IResourceService resourceService, IAssessmentQuestionRepository assessmentQuestionRepository, IConfiguration configuration)
        {
            _testResultRepository = testResultRepository;
            _testResultDetailRepository = testResultDetailRepository;
            _assessmentQuestionRepository = assessmentQuestionRepository;
            _mapper = mapper;
            _logger = logger;
            _resourceService = resourceService;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _response = new APIResponse();
        }

        [HttpPost("SubmitTest")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> SubmitTest(TestResultCreateDTO createDTO)
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

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllParentTestResult([FromQuery] string? search = "", string? orderBy = SD.CREATED_DATE, string? sort = SD.ORDER_DESC, int pageNumber = 1)
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
                int totalCount = 0;
                List<TestResult> list = new();
                Expression<Func<TestResult, bool>> filter = u => true;
                Expression<Func<TestResult, object>> orderByQuery = u => true;
                bool isDesc = sort != null && sort == SD.ORDER_DESC;

                filter = x => x.ParentId.Equals(userId);

                if (!string.IsNullOrEmpty(search))
                {
                    filter.AndAlso(u => u.Test.TestName.Contains(search));
                }

                if (orderBy != null)
                {
                    switch (orderBy)
                    {
                        case SD.CREATED_DATE:
                            orderByQuery = x => x.CreatedDate;
                            break;
                        case SD.POINT:
                            orderByQuery = x => x.TotalPoint;
                            break;
                        default:
                            orderByQuery = x => x.CreatedDate;
                            break;
                    }
                }

                var (count, result) = await _testResultRepository.GetAllAsync(filter, includeProperties: "Test",
                                                                                pageSize: pageSize,
                                                                                pageNumber: pageNumber,
                                                                                orderBy: orderByQuery,
                                                                                isDesc: isDesc);
                list = result;
                totalCount = count;

                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = totalCount };

                _response.Result = _mapper.Map<List<TestResultDTO>>(list);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;
                _response.IsSuccess = true;
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
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }

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
