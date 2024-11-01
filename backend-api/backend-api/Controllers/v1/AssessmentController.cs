using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Repository.IRepository;
using backend_api.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace backend_api.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class AssessmentController : ControllerBase
    {
        private readonly IAssessmentQuestionRepository _assessmentQuestionRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        protected ILogger<AssessmentController> _logger;
        private readonly IResourceService _resourceService;

        public AssessmentController(IAssessmentQuestionRepository assessmentQuestionRepository,
            IMapper mapper, ILogger<AssessmentController> logger,
            IResourceService resourceService)
        {
            _resourceService = resourceService;
            _logger = logger;
            _assessmentQuestionRepository = assessmentQuestionRepository;
            _response = new APIResponse();
            _mapper = mapper;
        }


        [HttpPost]
        [Authorize(Roles = SD.STAFF_ROLE)]
        public async Task<ActionResult<APIResponse>> CreateAsync([FromBody] AssessmentQuestionCreateDTO assessmentQuestionCreateDTO)
        {
            try
            {
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
                // TODO: Add submitter 
                model.IsAssessment = true;
                model.IsHidden = false;
                model.CreatedDate = DateTime.Now;
                var assessmentQuestion = await _assessmentQuestionRepository.CreateAsync(model);

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
                var result = await _assessmentQuestionRepository.GetAllNotPagingAsync(null, "AssessmentOptions", null);
                _response.Result = _mapper.Map<List<AssessmentQuestionDTO>>(result.list.OrderBy(x => x.Id).ToList());
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
