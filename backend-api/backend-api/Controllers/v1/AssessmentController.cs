using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Repository.IRepository;
using backend_api.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
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
        protected ILogger<AssessmentController> _logger;
        private readonly IMapper _mapper;
        private readonly IStringLocalizer<Messages> _localizer;

        public AssessmentController(IAssessmentQuestionRepository assessmentQuestionRepository,
            IMapper mapper, IStringLocalizer<Messages> localizer,
            ILogger<AssessmentController> logger)
        {
            _assessmentQuestionRepository = assessmentQuestionRepository;
            _response = new APIResponse();
            _mapper = mapper;
            _localizer = localizer;
            _logger = logger;
        }


        [HttpPost]
        //[Authorize(Roles = SD.STAFF_ROLE)]
        public async Task<ActionResult<APIResponse>> CreateAsync([FromBody] AssessmentQuestionCreateDTO assessmentQuestionCreateDTO)
        {
            try
            {
                if (assessmentQuestionCreateDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }

                var assessmentExist = await _assessmentQuestionRepository.GetAsync(x => x.Question.Equals(assessmentQuestionCreateDTO.Question));
                if (assessmentExist != null)
                {
                    _logger.LogWarning($"Duplicate question attempted: {assessmentQuestionCreateDTO.Question}");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    var message = string.Format(Resources.Messages.ResourceManager.GetString("DUPLICATED_MESSAGE"), assessmentQuestionCreateDTO.Question);
                    _response.ErrorMessages = new List<string> { message };
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
                _response.ErrorMessages = new List<string>() { _localizer["INTERAL_ERROR_MESSAGE"] };
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
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _localizer["INTERAL_ERROR_MESSAGE"] };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
