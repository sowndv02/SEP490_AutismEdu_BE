using AutoMapper;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Models.DTOs;
using AutismEduConnectSystem.Models.DTOs.CreateDTOs;
using AutismEduConnectSystem.Repository;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using AutismEduConnectSystem.Models.DTOs.UpdateDTOs;

namespace AutismEduConnectSystem.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class TestController : ControllerBase
    {
        private readonly ITestRepository _testRepository;
        private readonly IAssessmentQuestionRepository _assessmentQuestionRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        protected ILogger<TestController> _logger;
        private readonly IResourceService _resourceService;
        protected int pageSize = 0;

        public TestController(ITestRepository testRepository, IMapper mapper, ILogger<TestController> logger, 
            IResourceService resourceService, IConfiguration configuration, IAssessmentQuestionRepository assessmentQuestionRepository)
        {
            _testRepository = testRepository;
            _mapper = mapper;
            _logger = logger;
            _resourceService = resourceService;
            _response = new APIResponse();
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _assessmentQuestionRepository = assessmentQuestionRepository;
        }

        [HttpPost]
        [Authorize(Roles = $"{SD.STAFF_ROLE},{SD.MANAGER_ROLE}")]
        public async Task<ActionResult<APIResponse>> CreateAsync(TestCreateDTO createDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (createDTO == null)
                {
                    _logger.LogWarning("Received null TestCreateDTO object.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.TEST) };
                    return BadRequest(_response);
                }

                var testExist = await _testRepository.GetAsync(x => x.TestName.Equals(createDTO.TestName));
                if (testExist != null)
                {
                    _logger.LogWarning("Test with name {TestName} already exists.", createDTO.TestName);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.TEST) };
                    return BadRequest(_response);
                }

                Test model = _mapper.Map<Test>(createDTO);
                if (createDTO.OriginalId == null || createDTO.OriginalId == 0)
                {
                    model.OriginalId = null;
                }
                model.VersionNumber = await _testRepository.GetNextVersionNumberAsync(model.OriginalId);
                await _testRepository.DeactivatePreviousVersionsAsync(createDTO.OriginalId);
                model.IsHidden = true;
                model.CreatedBy = userId;
                model.CreatedDate = DateTime.Now;

                var test = await _testRepository.CreateAsync(model);

                _response.Result = _mapper.Map<TestDTO>(test);
                _response.IsSuccess = true;
                _response.StatusCode = HttpStatusCode.Created;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the test.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? search = "", string? orderBy = SD.CREATED_DATE, string? sort = SD.ORDER_DESC, int pageNumber = 1)
        {
            try
            {
                int totalCount = 0;
                List<Test> list = new();
                Expression<Func<Test, bool>> filter = u => true;
                Expression<Func<Test, object>> orderByQuery = u => true;
                bool isDesc = sort != null && sort == SD.ORDER_DESC;

                if (!string.IsNullOrEmpty(search))
                {
                    filter = filter.AndAlso(u => u.TestName.Contains(search));
                }

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

                var (count, result) = await _testRepository.GetAllAsync(filter, includeProperties: null, 
                                                                                pageSize: 9, 
                                                                                pageNumber: pageNumber, 
                                                                                orderBy: orderByQuery, 
                                                                                isDesc: isDesc);
                list = result;
                totalCount = count;

                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = totalCount };

                _response.Result = _mapper.Map<List<TestDTO>>(list);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching tests.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet("{testId}")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetTestById(int testId)
        {
            try
            {
                var test = await _testRepository.GetAsync(x => x.Id == testId);
                if (test == null)
                {
                    _logger.LogWarning("TestId {testId} doesn't exist.", testId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.TEST) };
                    return BadRequest(_response);
                }

                var model = _mapper.Map<TestDTO>(test);
                var questions = await _assessmentQuestionRepository.GetAllWithIncludeAsync(x => x.TestId == testId, "AssessmentOptions");
                model.Questions = _mapper.Map<List<AssessmentQuestionDTO>>(questions.list);

                _response.Result = model;
                _response.IsSuccess = true;
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching test.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPut("hideShowTest")]
        [Authorize(Roles = SD.MANAGER_ROLE)]
        public async Task<IActionResult> HideShow(HideShowDTO updateDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model state is invalid. Returning BadRequest.");

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.TEST) };
                    return BadRequest(_response);
                }

                var model = await _testRepository.GetAsync(x => x.Id == updateDTO.Id);
                if (model == null)
                {
                    _logger.LogWarning("Test with ID: {Id} is not found.", updateDTO.Id);

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.TEST) };
                    return BadRequest(_response);
                }
                model.IsHidden = updateDTO.IsHidden;
                model.UpdatedDate = DateTime.Now;
                await _testRepository.UpdateAsync(model);
                _response.Result = _mapper.Map<PackagePaymentDTO>(model);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while processing the status change for package payment ID: {Id}. Error: {Error}", updateDTO.Id, ex.Message);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
