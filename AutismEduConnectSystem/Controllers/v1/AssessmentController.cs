using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Models.DTOs;
using AutismEduConnectSystem.Models.DTOs.CreateDTOs;
using AutismEduConnectSystem.Models.DTOs.UpdateDTOs;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace AutismEduConnectSystem.Controllers.v1
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
        [Authorize(Roles = $"{SD.STAFF_ROLE}, {SD.MANAGER_ROLE}")]
        public async Task<ActionResult<APIResponse>> CreateAsync([FromBody] AssessmentQuestionCreateDTO assessmentQuestionCreateDTO)
        {
            try
            {

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }

                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                if (userRoles == null || (!userRoles.Contains(SD.STAFF_ROLE) && !userRoles.Contains(SD.MANAGER_ROLE)))
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }


                if (assessmentQuestionCreateDTO == null || !ModelState.IsValid)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ASSESSMENT_QUESTION) };
                    return BadRequest(_response);
                }

                var assessmentExist = await _assessmentQuestionRepository.GetAsync(x => x.Question.Equals(assessmentQuestionCreateDTO.Question));
                if (assessmentExist != null)
                {
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
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                var questions = await _assessmentQuestionRepository.GetAllNotPagingAsync(null, "AssessmentOptions", null);
                var scoreRanges = await _assessmentScoreRangeRepository.GetAllNotPagingAsync();

                foreach (var question in questions.list)
                {
                    question.AssessmentOptions = question.AssessmentOptions.OrderBy(x => x.Point).ToList();
                }

                AllAssessmentDTO model = new AllAssessmentDTO();
                model.Questions = _mapper.Map<List<AssessmentQuestionDTO>>(questions.list);
                model.ScoreRanges = _mapper.Map<List<AssessmentScoreRangeDTO>>(scoreRanges.list);
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


        [HttpPut]
        [Authorize(Roles = $"{SD.STAFF_ROLE}, {SD.MANAGER_ROLE}")]
        public async Task<ActionResult<APIResponse>> UpdateAsync([FromBody] AssessmentQuestionUpdateDTO updateDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }

                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                if (userRoles == null || (!userRoles.Contains(SD.STAFF_ROLE) && !userRoles.Contains(SD.MANAGER_ROLE)))
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }

                if (updateDTO == null || !ModelState.IsValid)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ASSESSMENT_QUESTION) };
                    return BadRequest(_response);
                }

                var model = await _assessmentQuestionRepository.GetAsync(x => x.Id == updateDTO.Id, true, "AssessmentOptions");
                if (model == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.ASSESSMENT_QUESTION) };
                    return NotFound(_response);
                }

                var assessmentExist = await _assessmentQuestionRepository.GetAsync(x => x.Id != updateDTO.Id && x.Question.Equals(updateDTO.Question));
                if (assessmentExist != null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, updateDTO.Question) };
                    return BadRequest(_response);
                }

                model.Question = updateDTO.Question;
                model.UpdatedDate = DateTime.Now;
                model.AssessmentOptions = _mapper.Map<List<AssessmentOption>>(updateDTO.AssessmentOptions);
                var assessmentQuestion = await _assessmentQuestionRepository.UpdateAsync(model);

                _response.Result = _mapper.Map<AssessmentQuestionDTO>(assessmentQuestion);
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
    }
}
