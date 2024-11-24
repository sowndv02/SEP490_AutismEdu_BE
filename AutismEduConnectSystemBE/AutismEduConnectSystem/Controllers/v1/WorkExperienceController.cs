using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Models.DTOs;
using AutismEduConnectSystem.Models.DTOs.CreateDTOs;
using AutismEduConnectSystem.Models.DTOs.UpdateDTOs;
using AutismEduConnectSystem.RabbitMQSender;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.Utils;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using static AutismEduConnectSystem.SD;

namespace AutismEduConnectSystem.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class WorkExperienceController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IWorkExperienceRepository _workExperienceRepository;
        private readonly IMapper _mapper;
        private string queueName = string.Empty;
        private readonly IRabbitMQMessageSender _messageBus;
        protected APIResponse _response;
        private readonly ILogger<WorkExperienceController> _logger;
        protected int pageSize = 0;
        private readonly IResourceService _resourceService;

        public WorkExperienceController(IUserRepository userRepository, IWorkExperienceRepository workExperienceRepository,
            IMapper mapper, IConfiguration configuration,
            IRabbitMQMessageSender messageBus, IResourceService resourceService, ILogger<WorkExperienceController> logger)
        {
            _messageBus = messageBus;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            queueName = configuration["RabbitMQSettings:QueueName"];
            _response = new APIResponse();
            _mapper = mapper;
            _userRepository = userRepository;
            _workExperienceRepository = workExperienceRepository;
            _resourceService = resourceService;
            _logger = logger;
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> DeleteAsync(int id)
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
            if (userRoles == null || (!userRoles.Contains(SD.TUTOR_ROLE)))
            {
               
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.Forbidden;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                return StatusCode((int)HttpStatusCode.Forbidden, _response);
            }

            try
            {

                if (id == 0)
                {
                    _logger.LogWarning($"Invalid ID {id} provided for deletion.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                var model = await _workExperienceRepository.GetAsync(x => x.Id == id && x.SubmitterId == userId, true, null, null);

                if (model == null)
                {
                    _logger.LogWarning($"Work experience with ID {id} not found for user {userId}.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.WORK_EXPERIENCE) };
                    return BadRequest(_response);
                }
                model.IsActive = false;
                model.IsDeleted = true;
                await _workExperienceRepository.UpdateAsync(model);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred while deleting work experience with ID {id} for user {userId}: {ex.Message}");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet]
        [Authorize(Roles = $"{SD.TUTOR_ROLE},{SD.MANAGER_ROLE},{SD.STAFF_ROLE}")]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? search, string? status = SD.STATUS_ALL, string? orderBy = SD.CREATED_DATE, string? sort = SD.ORDER_DESC, int pageNumber = 1)
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
                if (userRoles == null || (!userRoles.Contains(SD.MANAGER_ROLE) && !userRoles.Contains(SD.TUTOR_ROLE) && !userRoles.Contains(SD.STAFF_ROLE)))
                {
                   
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                Expression<Func<WorkExperience, bool>> filter = u => true;
                Expression<Func<WorkExperience, object>> orderByQuery = u => true;
                if (userRoles.Contains(SD.TUTOR_ROLE))
                {
                    filter = filter.AndAlso(x => !string.IsNullOrEmpty(x.SubmitterId) && x.SubmitterId == userId && !x.IsDeleted);
                }
                if (!string.IsNullOrEmpty(search))
                {
                    filter = filter.AndAlso(x => x.CompanyName.ToLower().Contains(search.ToLower()));
                }
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
                            filter = filter.AndAlso(x => x.RequestStatus == Status.APPROVE);
                            break;
                        case "reject":
                            filter = filter.AndAlso(x => x.RequestStatus == Status.REJECT);
                            break;
                        case "pending":
                            filter = filter.AndAlso(x => x.RequestStatus == Status.PENDING);
                            break;
                    }
                }
                var (count, result) = await _workExperienceRepository.GetAllAsync(filter,
                                "Submitter,TutorRegistrationRequest", pageSize: pageSize, pageNumber: pageNumber, orderByQuery, isDesc);
                foreach (var item in result)
                {
                    if (item.Submitter != null)
                    {
                        item.Submitter.User = await _userRepository.GetAsync(u => u.Id == item.SubmitterId, false, null);
                    }
                }

                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = count };
                _response.Result = _mapper.Map<List<WorkExperienceDTO>>(result);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred while fetching work experiences: {ex.Message}");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> CreateAsync(WorkExperienceCreateDTO createDTO)
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
                if (userRoles == null || (!userRoles.Contains(SD.TUTOR_ROLE)))
                {
                   
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model state is invalid. Returning BadRequest.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.WORK_EXPERIENCE) };
                    return BadRequest(_response);
                }
                var isExisted = await _workExperienceRepository.GetAllNotPagingAsync(x => createDTO.OriginalId != null && createDTO.OriginalId != 0 && x.OriginalId == createDTO.OriginalId && x.RequestStatus == SD.Status.PENDING, null, null, null, true);
                if (isExisted.TotalCount > 0)
                {
                    _logger.LogWarning("Cannot spam update workex");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.WORK_EXPERIENCE) };
                    return BadRequest(_response);
                }

                var newModel = _mapper.Map<WorkExperience>(createDTO);
                newModel.SubmitterId = userId;
                newModel.IsActive = false;
                newModel.VersionNumber = await _workExperienceRepository.GetNextVersionNumberAsync(createDTO.OriginalId);
                if (createDTO.OriginalId == 0)
                {
                    newModel.OriginalId = null;
                }
                await _workExperienceRepository.CreateAsync(newModel);
                _response.StatusCode = HttpStatusCode.Created;
                _response.Result = _mapper.Map<WorkExperienceDTO>(newModel);
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while creating work experience: {ex.Message}");
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPut("changeStatus/{id}")]
        [Authorize(Roles = $"{SD.STAFF_ROLE},{SD.MANAGER_ROLE}")]
        public async Task<ActionResult<APIResponse>> UpdateStatusRequest(ChangeStatusDTO changeStatusDTO)
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

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model state is invalid. Returning BadRequest.");

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.WORK_EXPERIENCE) };
                    return BadRequest(_response);
                }

                WorkExperience model = await _workExperienceRepository.GetAsync(x => x.Id == changeStatusDTO.Id, false, null, null);
                if (model == null || model.RequestStatus != Status.PENDING)
                {
                    _logger.LogWarning("Work experience with ID: {Id} is either not found or already processed. Returning BadRequest.", changeStatusDTO.Id);

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.WORK_EXPERIENCE) };
                    return BadRequest(_response);
                }
                var tutor = await _userRepository.GetAsync(x => x.Id == model.SubmitterId);
                if (changeStatusDTO.StatusChange == (int)Status.APPROVE)
                {
                    model.RequestStatus = Status.APPROVE;
                    model.UpdatedDate = DateTime.Now;
                    model.IsActive = true;
                    model.ApprovedId = userId;
                    await _workExperienceRepository.DeactivatePreviousVersionsAsync(model.OriginalId);
                    await _workExperienceRepository.UpdateAsync(model);

                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ChangeStatusTemplate.cshtml");
                    if (System.IO.File.Exists(templatePath) && tutor != null)
                    {
                        // Send mail
                        var subject = "Yêu cập nhật kinh nghiệm làm việc của bạn đã được chấp nhận!";
                        var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);

                        var rejectionReasonHtml = string.Empty;
                        var htmlMessage = templateContent
                        .Replace("@Model.FullName", tutor.FullName)
                        .Replace("@Model.IssueName", $"Yêu cầu cập nhật kinh nghiệm làm việc của bạn tại {model.CompanyName}")
                        .Replace("@Model.IsApprovedString", "Chấp nhận")
                        .Replace("@Model.RejectionReason", rejectionReasonHtml);

                        _messageBus.SendMessage(new EmailLogger()
                        {
                            UserId = tutor.Id,
                            Email = tutor.Email,
                            Subject = subject,
                            Message = htmlMessage
                        }, queueName);
                    }

                    _response.Result = _mapper.Map<WorkExperienceDTO>(model);
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    return Ok(_response);
                }
                else if (changeStatusDTO.StatusChange == (int)Status.REJECT)
                {
                    // Handle for reject
                    model.RejectionReason = changeStatusDTO.RejectionReason;
                    model.UpdatedDate = DateTime.Now;
                    model.RequestStatus = Status.REJECT;
                    model.ApprovedId = userId;
                    await _workExperienceRepository.UpdateAsync(model);

                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ChangeStatusTemplate.cshtml");
                    if (System.IO.File.Exists(templatePath) && tutor != null)
                    {
                        //Send mail
                        var subject = "Yêu cập nhật kinh nghiệm làm việc của bạn đã bị từ chối!";
                        var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);

                        var rejectionReasonHtml = $"<p><strong>Lý do từ chối:</strong> {changeStatusDTO.RejectionReason}</p>";
                        var htmlMessage = templateContent
                            .Replace("@Model.FullName", tutor.FullName)
                            .Replace("@Model.IssueName", $"Yêu cầu cập nhật kinh nghiệm làm việc của bạn tại {model.CompanyName}")
                            .Replace("@Model.IsApprovedString", "Từ chối")
                            .Replace("@Model.RejectionReason", rejectionReasonHtml);

                        _messageBus.SendMessage(new EmailLogger()
                        {
                            UserId = tutor.Id,
                            Email = tutor.Email,
                            Subject = subject,
                            Message = htmlMessage
                        }, queueName);
                    }


                    _response.Result = _mapper.Map<WorkExperienceDTO>(model);
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
                _logger.LogError("An error occurred while processing the status change for work experience ID: {Id}. Error: {Error}", changeStatusDTO.Id, ex.Message);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
