using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models.DTOs;
using backend_api.Repository;
using backend_api.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using System.Linq.Expressions;
using backend_api.Utils;
using MailKit.Search;

namespace backend_api.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class ProgressReportController : ControllerBase
    {
        protected APIResponse _response;
        private readonly IMapper _mapper;
        protected int pageSize = 0;
        private readonly IProgressReportRepository _progressReportRepository;
        private readonly IAssessmentResultRepository _assessmentResultRepository;
        private readonly IInitialAssessmentResultRepository _initialAssessmentResultRepository;

        public ProgressReportController(IMapper mapper, IConfiguration configuration,
            IProgressReportRepository progressReportRepository, IAssessmentResultRepository assessmentResultRepository,
            IInitialAssessmentResultRepository initialAssessmentResultRepository)
        {
            _response = new APIResponse();
            _mapper = mapper;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _progressReportRepository = progressReportRepository;
            _assessmentResultRepository = assessmentResultRepository;
            _initialAssessmentResultRepository = initialAssessmentResultRepository;
        }

        [HttpPost]
        public async Task<ActionResult<APIResponse>> CreateAsync(ProgressReportCreateDTO createDTO)
        {
            try
            {
                var tutorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (createDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }

                if (string.IsNullOrEmpty(tutorId))
                {
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return Unauthorized(_response);
                }

                var model = _mapper.Map<ProgressReport>(createDTO);
                model.TutorId = tutorId;
                model.CreatedDate = DateTime.Now;

                var progressReport = await _progressReportRepository.CreateAsync(model);
                List<AssessmentResult> assessmentResults = new List<AssessmentResult>();

                foreach (var assessmentResult in progressReport.AssessmentResults)
                {
                    assessmentResults.Add(await _assessmentResultRepository.GetAsync(x => x.Id == assessmentResult.Id, true, "Question,Option"));
                }
                progressReport.AssessmentResults = assessmentResults;

                _response.Result = _mapper.Map<ProgressReportDTO>(progressReport);
                _response.StatusCode = HttpStatusCode.Created;
                _response.IsSuccess = true;
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
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] int studentProfileId, DateTime? startDate = null, DateTime? endDate = null, string? orderBy = SD.CREADTED_DATE, string? sort = SD.ORDER_DESC, int pageNumber = 1, bool getInitialResult = false)
        {
            try
            {
                int totalCount = 0;
                List<ProgressReport> list = new();
                Expression<Func<ProgressReport, bool>> filter = u => true;
                Expression<Func<ProgressReport, object>> orderByQuery = u => true;
                bool isDesc = sort != null && sort == SD.ORDER_DESC;

                filter = u => u.StudentProfileId == studentProfileId;

                if (orderBy != null)
                {
                    switch (orderBy)
                    {
                        case SD.CREADTED_DATE:
                            orderByQuery = x => x.CreatedDate;
                            break;
                        default:
                            orderByQuery = x => x.CreatedDate;
                            break;
                    }
                }

                if (startDate != null)
                {
                    filter = filter.AndAlso(u => u.CreatedDate.Date >= startDate.Value.Date);
                }
                if (endDate != null)
                {
                    filter = filter.AndAlso(u => u.CreatedDate.Date <= endDate.Value.Date);
                }


                var (count, result) = await _progressReportRepository.GetAllAsync(filter,
                                "StudentProfile,AssessmentResults", pageSize: pageSize, pageNumber: pageNumber, orderByQuery, isDesc);
                list = result;
                totalCount = count;
                
                foreach (var item in list)
                {
                    List<AssessmentResult> assessmentResults = new List<AssessmentResult>();
                    foreach (var assessmentResult in item.AssessmentResults)
                    {
                        assessmentResults.Add(await _assessmentResultRepository.GetAsync(x => x.Id == assessmentResult.Id, true, "Question,Option"));
                    }
                    item.AssessmentResults = assessmentResults;
                }

                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = totalCount };

                if (getInitialResult)
                {
                    ProgressReportGraphDTO graph = new ProgressReportGraphDTO();
                    graph.ProgressReports = _mapper.Map<List<ProgressReportDTO>>(list);

                    var initialAssessmentResult = await _initialAssessmentResultRepository.GetAllAsync(x => x.StudentProfileId == studentProfileId, "Question,Option");
                    graph.InitialAssessmentResultDTO = _mapper.Map<InitialAssessmentResultDTO>(initialAssessmentResult.list);

                    _response.Result = graph;
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.Pagination = pagination;
                    return Ok(_response);
                }

                _response.Result = _mapper.Map<List<ProgressReportDTO>>(list);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;
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

        [HttpGet("{Id}")]
        public async Task<ActionResult<APIResponse>> GetProgressReportById(int Id)
        {
            try
            {
                var progressReport = await _progressReportRepository.GetAsync(x => x.Id == Id,true, "AssessmentResults");

                if (progressReport == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.NOT_FOUND_MESSAGE };
                    return BadRequest(_response);
                }

                List<AssessmentResult> assessmentResults = new List<AssessmentResult>();

                foreach (var assessmentResult in progressReport.AssessmentResults)
                {
                    assessmentResults.Add(await _assessmentResultRepository.GetAsync(x => x.Id == assessmentResult.Id, true, "Question,Option"));
                }
                progressReport.AssessmentResults = assessmentResults;

                _response.Result = _mapper.Map<ProgressReportDTO>(progressReport);
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
