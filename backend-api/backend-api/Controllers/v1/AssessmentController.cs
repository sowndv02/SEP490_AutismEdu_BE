using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Repository.IRepository;
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
        private readonly IAssessmentOptionRepository _assessmentOptionRepository;
        private readonly IAssessmentResultRepository _assessmentResultRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;

        public AssessmentController(IAssessmentQuestionRepository assessmentQuestionRepository, 
            IAssessmentOptionRepository assessmentOptionRepository, IAssessmentResultRepository assessmentResultRepository, 
            IMapper mapper, IConfiguration configuration)
        {
            _assessmentQuestionRepository = assessmentQuestionRepository;
            _assessmentOptionRepository = assessmentOptionRepository;
            _assessmentResultRepository = assessmentResultRepository;
            _response = new APIResponse();
            _mapper = mapper;
        }

        [HttpPost]
        //[Authorize]
        public async Task<ActionResult<APIResponse>> CreateAsync([FromBody]AssessmentQuestionCreateDTO assessmentQuestionCreateDTO)
        {
            try
            {
                if(assessmentQuestionCreateDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }
                AssessmentQuestion model = _mapper.Map<AssessmentQuestion>(assessmentQuestionCreateDTO);
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
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet]
        //[Authorize]
        public async Task<ActionResult<APIResponse>> GetAllAsync()
        {
            try
            {
                var result = await _assessmentQuestionRepository.GetAllNotPagingAsync(null, "AssessmentOptions", null);

                _response.Result = _mapper.Map<List<AssessmentQuestionDTO>>(result.list.OrderBy(x => x.Id));
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
