using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs.CreateDTOs;
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
    public class TestController : ControllerBase
    {
        private readonly ITestRepository _testRepository;
        private readonly ITestResultRepository _resultRepository;
        private readonly ITestResultDetailRepository _resultDetailRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        protected ILogger<AssessmentScoreRange> _logger;
        private readonly IResourceService _resourceService;

        public TestController(ITestRepository testRepository, ITestResultRepository resultRepository, 
            ITestResultDetailRepository resultDetailRepository, IMapper mapper, ILogger<AssessmentScoreRange> logger, 
            IResourceService resourceService)
        {
            _testRepository = testRepository;
            _resultRepository = resultRepository;
            _resultDetailRepository = resultDetailRepository;
            _mapper = mapper;
            _logger = logger;
            _resourceService = resourceService;
            _response = new APIResponse();
        }

        [HttpPost]
        //[Authorize(Roles = SD.STAFF_ROLE)]
        public async Task<ActionResult<APIResponse>> CreateAsync(TestCreateDTO createDTO)
        {
            try
            {
                // TODO: remove fixed id
                var userId = "";//User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (createDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.TEST) };
                    return BadRequest(_response);
                }

                var testExist = await _testRepository.GetAsync(x => x.TestName.Equals(createDTO.TestName));
                if (testExist != null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.TEST) };
                    return BadRequest(_response);
                }

                Test model = _mapper.Map<Test>(createDTO);
                model.CreatedBy = userId;
                model.CreatedDate = DateTime.Now;

                var test = await _testRepository.CreateAsync(model);

                _response.Result = test;
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
    }
}
