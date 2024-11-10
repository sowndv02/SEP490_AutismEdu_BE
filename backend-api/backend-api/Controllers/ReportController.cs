using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models.DTOs.UpdateDTOs;
using backend_api.RabbitMQSender;
using backend_api.Repository;
using backend_api.Repository.IRepository;
using backend_api.Services.IServices;
using backend_api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Tls;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using static backend_api.SD;

namespace backend_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiVersionNeutral]
    public class ReportController : ControllerBase
    {
        private readonly IReportRepository _reportRepository;
        private readonly IReportMediaRepository _reportMediaRepository;
        private readonly IBlobStorageRepository _blobStorageRepository ;
        private readonly IStudentProfileRepository _studentProfileRepository;
        private readonly IChildInformationRepository _childInformationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        private object blog;
        private readonly ILogger<ReportController> _logger;
        private readonly IResourceService _resourceService;
        public ReportController(IReportRepository reportRepository, 
            IReportMediaRepository reportMediaRepository,
            IMapper mapper, IResourceService resourceService,
            ILogger<ReportController> logger, 
            IStudentProfileRepository studentProfileRepository, 
            IChildInformationRepository childInformationRepository,
            IBlobStorageRepository blobStorageRepository,
            IUserRepository userRepository)
        {
            _userRepository = userRepository;
            _blobStorageRepository = blobStorageRepository;
            _childInformationRepository = childInformationRepository;
            _studentProfileRepository = studentProfileRepository;
            _reportMediaRepository = reportMediaRepository; 
            _response = new APIResponse();
            _mapper = mapper;
            _reportRepository = reportRepository;
            _resourceService = resourceService;
            _logger = logger;
        }


        [HttpPost("AppealBan")]
        public async Task<ActionResult<APIResponse>> CreateReportAppealBanAsync(ReportAppealBanCreateDTO createDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model state is invalid. Returning BadRequest.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.REPORT) };
                    return BadRequest(_response);
                }
                var existReport = await _reportRepository.GetAllNotPagingAsync(x => x.ReportType == SD.ReportType.UNLOCKREQUEST && x.Email == createDTO.Email && x.Status == SD.Status.PENDING);
                if (existReport.list != null && !existReport.list.Any())
                {
                    _logger.LogWarning("Cannot spam report");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.REPORT) };
                    return BadRequest(_response);
                }
                var user = await _userRepository.GetAsync(x => x.Email == createDTO.Email);
                if (user != null && user.LockoutEnd <= DateTime.Now)
                {
                    _logger.LogWarning("Cannot spam report");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.REPORT) };
                    return BadRequest(_response);
                }
                var newModel = _mapper.Map<Report>(createDTO);

                if(user != null) newModel.ReporterId = user.Id;
                newModel.ReportType = SD.ReportType.UNLOCKREQUEST;
                newModel.Title = SD.REPORT_UNLOCKREQUEST_TITLE;

                var reportModel = await _reportRepository.CreateAsync(newModel);
                var result = await _reportRepository.GetAsync(x => x.Id == reportModel.Id, false, "Reporter", null);

                _response.StatusCode = HttpStatusCode.Created;
                _response.Result = _mapper.Map<ReportAppealBanDTO>(result);
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while creating payment history: {ex.Message}");
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPost("review")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> CreateReportReviewAsync(ReportReviewCreateDTO createDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model state is invalid. Returning BadRequest.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.REPORT) };
                    return BadRequest(_response);
                }
                var existReport = await _reportRepository.GetAllNotPagingAsync(x => x.ReportType == SD.ReportType.REVIEW && x.ReporterId == userId && x.Status == SD.Status.PENDING);
                if(existReport.list != null && !existReport.list.Any())
                {
                    _logger.LogWarning("Cannot spam report");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.REPORT) };
                    return BadRequest(_response);
                }
                var newModel = _mapper.Map<Report>(createDTO);

                newModel.ReporterId = userId;
                newModel.ReportType = SD.ReportType.REVIEW;
                newModel.Title = SD.REPORT_REVIEW_TITLE;

                var reportModel = await _reportRepository.CreateAsync(newModel);
                var result = await _reportRepository.GetAsync(x => x.Id == reportModel.Id, false, "Review,Reporter", null);
                _response.StatusCode = HttpStatusCode.Created;
                _response.Result = _mapper.Map<ReportReviewDTO>(result);
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while creating payment history: {ex.Message}");
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPost("tutor")]
        [Authorize(Roles = SD.PARENT_ROLE)]
        public async Task<ActionResult<APIResponse>> CreateReportTutorAsync(ReportTutorCreateDTO createDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model state is invalid. Returning BadRequest.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.REPORT) };
                    return BadRequest(_response);
                }
                var existReport = await _reportRepository.GetAllNotPagingAsync(x => x.ReportType == SD.ReportType.TUTOR && x.ReporterId == userId && x.Status == SD.Status.PENDING);
                if (existReport.list != null && !existReport.list.Any())
                {
                    _logger.LogWarning("Cannot spam report");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.REPORT) };
                    return BadRequest(_response);
                }
                var child = await _childInformationRepository.GetAllNotPagingAsync(x => x.ParentId == userId);
                if (child.list != null && !child.list.Any())
                {
                    foreach (var item in child.list) 
                    {
                        var studentProfiles = await _studentProfileRepository.GetAllNotPagingAsync(x => x.ChildId == item.Id);
                        if(studentProfiles.list != null && !studentProfiles.list.Any())
                        {
                            if (studentProfiles.list.Any(x => x.TutorId == createDTO.TutorId))
                            {
                                _logger.LogWarning("Cannot report tutor");
                                _response.StatusCode = HttpStatusCode.BadRequest;
                                _response.IsSuccess = false;
                                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.REPORT) };
                                return BadRequest(_response);
                            }
                        }
                    }
                }

                var newModel = _mapper.Map<Report>(createDTO);

                newModel.ReporterId = userId;
                newModel.ReportType = SD.ReportType.TUTOR;
                newModel.Title = SD.REPORT_TUTOR_TITLE;

                var reportModel = await _reportRepository.CreateAsync(newModel);

                foreach (var media in createDTO.ReportMedias)
                {
                    using var stream = media.OpenReadStream();
                    var url = await _blobStorageRepository.Upload(stream, string.Concat(Guid.NewGuid().ToString(), Path.GetExtension(media.FileName)));
                    var objMedia = new ReportMedia() { ReportId = reportModel.Id, UrlMedia = url, CreatedDate = DateTime.Now };
                    await _reportMediaRepository.CreateAsync(objMedia);
                }

                var result = await _reportRepository.GetAsync(x => x.Id == reportModel.Id, false, "Tutor,Reporter,ReportMedias", null);
                _response.StatusCode = HttpStatusCode.Created;
                _response.Result = _mapper.Map<ReportTutorDTO>(result);
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while creating payment history: {ex.Message}");
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpGet]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? search, string? status = SD.STATUS_ALL, DateTime? startDate = null, DateTime? endDate = null, string? type = SD.TYPE_ALL, string? orderBy = SD.CREATED_DATE, string? sort = SD.ORDER_DESC, int pageNumber = 1)
        {
            try
            {
                int totalCount = 0;
                List<Report> list = new();
                Expression<Func<Report, bool>> filter = u => true;
                Expression<Func<Report, object>> orderByQuery = u => true;
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

                if (userRoles.Contains(SD.PARENT_ROLE) || userRoles.Contains(SD.TUTOR_ROLE))
                {
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    filter = u => !string.IsNullOrEmpty(u.ReporterId) && u.ReporterId == userId;
                }

                if (search != null && !string.IsNullOrEmpty(search))
                    filter = filter.AndAlso(x => x.Tutor.User != null && (x.Tutor.User.Email.ToLower().Contains(search.ToLower()) || x.Tutor.User.FullName.Contains(search.ToLower())) );
                
                if (startDate != null) 
                    filter.AndAlso(x => x.CreatedDate.Date >= startDate.Value.Date);
                
                if (endDate != null)
                    filter.AndAlso(x => x.CreatedDate.Date <= endDate.Value.Date);

                bool isDesc = !string.IsNullOrEmpty(sort) && sort == SD.ORDER_DESC;
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
                else
                {
                    orderByQuery = x => x.CreatedDate;
                }

                if (!string.IsNullOrEmpty(status) && status != SD.STATUS_ALL)
                {
                    switch (status.ToLower())
                    {
                        case "approve":
                            filter = filter.AndAlso(x => x.Status == Status.APPROVE);
                            break;
                        case "reject":
                            filter = filter.AndAlso(x => x.Status == Status.REJECT);
                            break;
                        case "pending":
                            filter = filter.AndAlso(x => x.Status == Status.PENDING);
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(type) && type != SD.TYPE_ALL)
                {
                    switch (type.ToLower())
                    {
                        case "review":
                            filter = filter.AndAlso(x => x.ReportType == ReportType.REVIEW);
                            break;
                        case "tutor":
                            filter = filter.AndAlso(x => x.ReportType == ReportType.TUTOR);
                            break;
                        case "account":
                            filter = filter.AndAlso(x => x.ReportType == ReportType.UNLOCKREQUEST);
                            break;
                    }
                }

                var (count, result) = await _reportRepository.GetAllWithIncludeAsync(
                    filter,
                    "Handler,Tutor,Review,Reporter,ReportMedias",
                    pageSize: 10,
                    pageNumber: pageNumber,
                    orderByQuery,
                    isDesc
                );

                list = result;
                totalCount = count;
                
                // Setup pagination and response
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = 10, Total = totalCount };
                _response.Result = _mapper.Map<List<ReportDTO>>(list);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetAllAsync Report");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPut("changeStatus/{id}")]
        [Authorize(Roles = $"{SD.STAFF_ROLE},{SD.MANAGER_ROLE}")]
        public async Task<IActionResult> ApproveOrRejectRequest(ReportUpdateDTO updateDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model state is invalid. Returning BadRequest.");

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.REPORT) };
                    return BadRequest(_response);
                }

                Report model = await _reportRepository.GetAsync(x => x.Id == updateDTO.Id, false, "Reporter", null);
                List<Report> reports = new();
                if (model == null || model.Status != Status.PENDING)
                {
                    _logger.LogWarning("Report with ID: {Id} is either not found or already processed. Returning BadRequest.", updateDTO.Id);

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.REPORT) };
                    return BadRequest(_response);
                }
                
                if (updateDTO.StatusChange == (int)Status.APPROVE)
                {
                    model.Status = Status.APPROVE;
                    model.UpdatedDate = DateTime.Now;
                    model.Comments = updateDTO.Comment;
                    model.HandlerId = userId;
                    await _reportRepository.UpdateAsync(model);
                    if (!string.IsNullOrEmpty(model.TutorId))
                    {
                        var (countReportTutor, listReportTutor) = await _reportRepository.GetAllNotPagingAsync(x => x.TutorId == model.TutorId && x.Id != model.Id, null, null, x => x.CreatedDate, true);
                        reports = listReportTutor;
                    }
                    else if (model.ReviewId != null && model.ReviewId > 0)
                    {
                        var (countReportReview, listReportReview) = await _reportRepository.GetAllNotPagingAsync(x => x.ReviewId == model.ReviewId && x.Id != model.Id, null, null, x => x.CreatedDate, true);
                        reports = listReportReview;
                    }
                    foreach(var item in reports)
                    {
                        item.Status = Status.APPROVE;
                        item.UpdatedDate = DateTime.Now;
                        item.Comments = updateDTO.Comment;
                        item.HandlerId = userId;
                        await _reportRepository.UpdateAsync(item);
                    }
                    _response.Result = _mapper.Map<ReportDTO>(model);
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    return Ok(_response);
                }
                else if (updateDTO.StatusChange == (int)Status.REJECT)
                {
                    // Handle for reject
                    model.Status = Status.REJECT;
                    model.UpdatedDate = DateTime.Now;
                    model.Comments = updateDTO.Comment;
                    model.HandlerId = userId;

                    await _reportRepository.UpdateAsync(model);
                    if (!string.IsNullOrEmpty(model.TutorId))
                    {
                        var (countReportTutor, listReportTutor) = await _reportRepository.GetAllNotPagingAsync(x => x.TutorId == model.TutorId && x.Id != model.Id, null, null, x => x.CreatedDate, true);
                        reports = listReportTutor;
                    }
                    else if (model.ReviewId != null && model.ReviewId > 0)
                    {
                        var (countReportReview, listReportReview) = await _reportRepository.GetAllNotPagingAsync(x => x.ReviewId == model.ReviewId && x.Id != model.Id, null, null, x => x.CreatedDate, true);
                        reports = listReportReview;
                    }
                    foreach (var item in reports)
                    {
                        item.Status = Status.APPROVE;
                        item.UpdatedDate = DateTime.Now;
                        item.Comments = updateDTO.Comment;
                        item.HandlerId = userId;
                        await _reportRepository.UpdateAsync(item);
                    }
                    _response.Result = _mapper.Map<ReportDTO>(model);
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    return Ok(_response);
                }
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while processing the status change for work experience ID: {Id}. Error: {Error}", updateDTO.Id, ex.Message);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpGet("{id}")]
        [Authorize(Roles = $"{SD.MANAGER_ROLE},{SD.STAFF_ROLE}")]
        public async Task<ActionResult<APIResponse>> GetByIdAsync(int id)
        {
            try
            {
                Report report = await _reportRepository.GetAsync(x => x.Id == id, false, "Handler,Tutor,Review,Reporter,ReportMedias", null);
                List<Report> reports = new();
                if (report == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.REPORT) };
                    return BadRequest(_response);
                }
                if (!string.IsNullOrEmpty(report.TutorId))
                {
                    var (countReportTutor, listReportTutor) = await _reportRepository.GetAllNotPagingAsync(x => x.TutorId == report.TutorId && x.Id != report.Id, "Handler,Reporter,ReportMedias", null, x => x.CreatedDate, true);
                    reports = listReportTutor;
                }
                else if (report.ReviewId != null && report.ReviewId > 0) 
                {
                    var (countReportReview, listReportReview) = await _reportRepository.GetAllNotPagingAsync(x => x.ReviewId == report.ReviewId && x.Id != report.Id, "Handler,Reporter", null, x => x.CreatedDate, true);
                    reports = listReportReview;
                }
                _response.Result = new { Result = _mapper.Map<ReportDTO>(report) , ReportsRelated = _mapper.Map<List<ReportDTO>>(reports) };
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while processing the retreive for report ID: {Id}. Error: {Error}", id, ex.Message);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
