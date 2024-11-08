using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models.DTOs.UpdateDTOs;
using backend_api.RabbitMQSender;
using backend_api.Repository.IRepository;
using backend_api.Services.IServices;
using backend_api.Utils;
using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using static backend_api.SD;

namespace backend_api.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class CurriculumController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ITutorRepository _tutorRepository;
        private readonly ICurriculumRepository _curriculumRepository;
        private readonly IMapper _mapper;
        private string queueName = string.Empty;
        private readonly IRabbitMQMessageSender _messageBus;
        private readonly ILogger<CurriculumController> _logger;
        protected APIResponse _response;
        protected int pageSize = 0;
        private readonly IResourceService _resourceService;

        public CurriculumController(IUserRepository userRepository, ITutorRepository tutorRepository,
            IMapper mapper, IConfiguration configuration, ILogger<CurriculumController> logger,
            ICurriculumRepository curriculumRepository, IRabbitMQMessageSender messageBus, IResourceService resourceService)
        {
            _logger = logger;
            _messageBus = messageBus;
            _curriculumRepository = curriculumRepository;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            queueName = configuration["RabbitMQSettings:QueueName"];
            _response = new APIResponse();
            _mapper = mapper;
            _userRepository = userRepository;
            _tutorRepository = tutorRepository;
            _resourceService = resourceService;
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> DeleteAsync(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (id == 0)
                {
                    _logger.LogWarning("Invalid curriculum ID: {CurriculumId}. Returning BadRequest.", id);
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                var model = await _curriculumRepository.GetAsync(x => x.Id == id && x.SubmitterId == userId, false, null);

                if (model == null)
                {
                    _logger.LogWarning("Curriculum not found for ID: {CurriculumId} and User ID: {UserId}. Returning BadRequest.", id, userId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.CURRICULUM) };
                    return BadRequest(_response);
                }
                model.IsActive = false;
                model.IsDeleted = true;
                await _curriculumRepository.UpdateAsync(model);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting curriculum ID: {CurriculumId}", id);
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return _response;
            }

        }


        [HttpGet("updateRequest")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? status = SD.STATUS_ALL, string? orderBy = SD.CREATED_DATE, string? sort = SD.ORDER_DESC, int pageNumber = 1)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int totalCount = 0;
                List<Curriculum> list = new();
                Expression<Func<Curriculum, bool>> filter = u => u.SubmitterId == userId && !u.IsDeleted;
                Expression<Func<Curriculum, object>> orderByQuery = u => true;

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
                var (count, result) = await _curriculumRepository.GetAllAsync(filter: filter, includeProperties: null, pageSize: 5, pageNumber: pageNumber, orderBy: orderByQuery, isDesc: isDesc);
                list = result;
                totalCount = count;
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = 5, Total = totalCount };
                _response.Result = _mapper.Map<List<CurriculumDTO>>(list);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching curricula for user ID: {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? search, string? status = SD.STATUS_ALL, string? orderBy = SD.CREATED_DATE, string? sort = SD.ORDER_DESC, int pageNumber = 1)
        {
            try
            {
                int totalCount = 0;
                List<Curriculum> list = new();
                Expression<Func<Curriculum, bool>> filter = u => true;
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                if (userRoles.Contains(SD.TUTOR_ROLE))
                {
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    Expression<Func<Curriculum, bool>> searchByTutor = u => !string.IsNullOrEmpty(u.SubmitterId) && u.SubmitterId == userId && !u.IsDeleted;

                    var combinedFilter = Expression.Lambda<Func<Curriculum, bool>>(
                        Expression.AndAlso(filter.Body, Expression.Invoke(searchByTutor, filter.Parameters)),
                        filter.Parameters
                    );
                    filter = combinedFilter;
                }
                bool isDesc = !string.IsNullOrEmpty(sort) && sort == SD.ORDER_DESC;
                Expression<Func<Curriculum, object>>? orderByQuery = null;

                if (orderBy != null)
                {
                    switch (orderBy)
                    {
                        case SD.CREATED_DATE:
                            orderByQuery = x => x.CreatedDate;
                            break;
                        case SD.AGE_FROM:
                            orderByQuery = x => x.AgeFrom;
                            break;
                        default:
                            orderByQuery = x => x.CreatedDate;
                            break;
                    }
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
                var (count, result) = await _curriculumRepository.GetAllAsync(filter,
                                "Submitter", pageSize: pageSize, pageNumber: pageNumber, orderByQuery, isDesc);
                list = result;
                totalCount = count;

                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = totalCount };
                _response.Result = _mapper.Map<List<CurriculumDTO>>(list);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching curricula for user ID: {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<IActionResult> CreateAsync(CurriculumCreateDTO curriculumDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for CreateAsync method. Model state errors: {@ModelState}", ModelState);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.CURRICULUM) };
                    return BadRequest(_response);
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var (total, list) = await _curriculumRepository.GetAllNotPagingAsync(x => x.AgeFrom <= curriculumDto.AgeFrom && x.AgeEnd >= curriculumDto.AgeEnd && x.SubmitterId == userId && !x.IsDeleted && x.IsActive);
                foreach (var item in list)
                {
                    if (item.AgeFrom == curriculumDto.AgeFrom || item.AgeEnd == curriculumDto.AgeEnd)
                    {
                        _logger.LogWarning("Duplicate age range found for AgeFrom: {AgeFrom} and AgeEnd: {AgeEnd}", curriculumDto.AgeFrom, curriculumDto.AgeEnd);
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.AGE) };
                        return BadRequest(_response);
                    }
                }
                var newCurriculum = _mapper.Map<Curriculum>(curriculumDto);

                newCurriculum.SubmitterId = userId;
                newCurriculum.IsActive = false;
                newCurriculum.VersionNumber = await _curriculumRepository.GetNextVersionNumberAsync(curriculumDto.OriginalCurriculumId);
                if (curriculumDto.OriginalCurriculumId == 0)
                {
                    newCurriculum.OriginalCurriculumId = null;
                }
                await _curriculumRepository.CreateAsync(newCurriculum);
                _response.StatusCode = HttpStatusCode.Created;
                _response.Result = _mapper.Map<CurriculumDTO>(newCurriculum);
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating curriculum for user ID: {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPut("changeStatus/{id}")]
        [Authorize(Roles = SD.STAFF_ROLE)]
        public async Task<IActionResult> ApproveOrRejectCurriculumRequest(ChangeStatusDTO changeStatusDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                Curriculum model = await _curriculumRepository.GetAsync(x => x.Id == changeStatusDTO.Id, false, null, null);
                var tutor = await _userRepository.GetAsync(x => x.Id == model.SubmitterId);
                if (model == null || model.RequestStatus != Status.PENDING)
                {
                    _logger.LogWarning("Curriculum not found for ID: {CurriculumId}", changeStatusDTO.Id);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.CURRICULUM) };
                    return BadRequest(_response);
                }
                if (model.RequestStatus != Status.PENDING)
                {
                    _logger.LogWarning("Curriculum ID: {CurriculumId} has already been processed with status: {RequestStatus}", changeStatusDTO.Id, model.RequestStatus);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.CURRICULUM) };
                    return BadRequest(_response);
                }
                if (changeStatusDTO.StatusChange == (int)Status.APPROVE)
                {
                    model.RequestStatus = Status.APPROVE;
                    model.UpdatedDate = DateTime.Now;
                    model.IsActive = true;
                    model.ApprovedId = userId;
                    await _curriculumRepository.DeactivatePreviousVersionsAsync(model.OriginalCurriculumId);
                    await _curriculumRepository.UpdateAsync(model);

                    // Send mail
                    var subject = "Yêu cập nhật khung chương trình của bạn đã được chấp nhận!";
                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ChangeStatusTemplate.cshtml");
                    var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);
                    var htmlMessage = templateContent
                        .Replace("@Model.FullName", tutor.FullName)
                        .Replace("@Model.IssueName", $"Yêu cầu cập nhật khung chương trình của bạn")
                        .Replace("@Model.IsApproved", Status.APPROVE.ToString());
                    _messageBus.SendMessage(new EmailLogger()
                    {
                        Email = tutor.Email,
                        Message = htmlMessage,
                        Subject = subject
                    }, queueName);

                    _response.Result = _mapper.Map<CurriculumDTO>(model);
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    return Ok(_response);
                }
                else if (changeStatusDTO.StatusChange == (int)Status.REJECT)
                {
                    // Handle for reject
                    model.RejectionReason = changeStatusDTO.RejectionReason;
                    model.RequestStatus = Status.REJECT;
                    model.UpdatedDate = DateTime.Now;
                    model.ApprovedId = userId;
                    await _curriculumRepository.UpdateAsync(model);

                    // Send mail
                    var subject = "Yêu cập nhật khung chương trình của bạn đã bị từ chối!";
                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ChangeStatusTemplate.cshtml");
                    var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);
                    var htmlMessage = templateContent
                        .Replace("@Model.FullName", tutor.FullName)
                        .Replace("@Model.IssueName", $"Yêu cầu cập nhật khung chương trình của bạn")
                        .Replace("@Model.IsApproved", Status.REJECT.ToString())
                        .Replace("@Model.RejectionReason", changeStatusDTO.RejectionReason);
                    _messageBus.SendMessage(new EmailLogger()
                    {
                        Email = tutor.Email,
                        Subject = subject,
                        Message = htmlMessage
                    }, queueName);

                    _response.Result = _mapper.Map<CurriculumDTO>(model);
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
                _logger.LogError(ex, "Error occurred while approving or rejecting curriculum request for curriculum ID: {CurriculumId}", changeStatusDTO.Id);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
