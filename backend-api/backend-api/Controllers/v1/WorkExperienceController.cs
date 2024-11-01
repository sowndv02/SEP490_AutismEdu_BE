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
    public class WorkExperienceController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IWorkExperienceRepository _workExperienceRepository;
        private readonly IMapper _mapper;
        private string queueName = string.Empty;
        private readonly IRabbitMQMessageSender _messageBus;
        protected APIResponse _response;
        protected int pageSize = 0;
        private readonly IResourceService _resourceService;

        public WorkExperienceController(IUserRepository userRepository, IWorkExperienceRepository workExperienceRepository,
            IMapper mapper, IConfiguration configuration,
            IRabbitMQMessageSender messageBus, IResourceService resourceService)
        {
            _messageBus = messageBus;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            queueName = configuration.GetValue<string>("RabbitMQSettings:QueueName");
            _response = new APIResponse();
            _mapper = mapper;
            _userRepository = userRepository;
            _workExperienceRepository = workExperienceRepository;
            _resourceService = resourceService;
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> DeleteAsync(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (id == 0)
                {
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                var model = await _workExperienceRepository.GetAsync(x => x.Id == id && x.SubmiterId == userId, false, null);

                if (model == null)
                {
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
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
            }
            return _response;
        }

        [HttpGet("updateRequest")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? status = SD.STATUS_ALL, string? orderBy = SD.CREADTED_DATE, string? sort = SD.ORDER_DESC, int pageNumber = 1)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int totalCount = 0;
                List<WorkExperience> list = new();
                Expression<Func<WorkExperience, bool>> filter = u => u.SubmiterId == userId && !u.IsDeleted;
                Expression<Func<WorkExperience, object>> orderByQuery = u => true;

                bool isDesc = !string.IsNullOrEmpty(orderBy) && orderBy == SD.ORDER_DESC;

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
                var (count, result) = await _workExperienceRepository.GetAllAsync(filter: filter, includeProperties: null, pageSize: 5, pageNumber: pageNumber, orderBy: orderByQuery, isDesc: isDesc);
                list = result;
                totalCount = count;
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = 5, Total = totalCount };
                _response.Result = _mapper.Map<List<WorkExperienceDTO>>(list);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;
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

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? search, string? status = SD.STATUS_ALL, string? orderBy = SD.CREADTED_DATE, string? sort = SD.ORDER_DESC, int pageNumber = 1)
        {
            try
            {
                int totalCount = 0;
                List<WorkExperience> list = new();
                Expression<Func<WorkExperience, bool>> filter = u => true;
                Expression<Func<WorkExperience, object>> orderByQuery = u => true;
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                if (userRoles.Contains(SD.TUTOR_ROLE))
                {
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    Expression<Func<WorkExperience, bool>> searchByTutor = u => !string.IsNullOrEmpty(u.SubmiterId) && u.SubmiterId == userId && !u.IsDeleted;

                    var combinedFilter = Expression.Lambda<Func<WorkExperience, bool>>(
                        Expression.AndAlso(filter.Body, Expression.Invoke(searchByTutor, filter.Parameters)),
                        filter.Parameters
                    );
                    filter = combinedFilter;
                }
                bool isDesc = !string.IsNullOrEmpty(sort) && sort == SD.ORDER_DESC;
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
                                "Submiter", pageSize: pageSize, pageNumber: pageNumber, orderByQuery, isDesc);
                list = result;
                totalCount = count;

                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = totalCount };
                _response.Result = _mapper.Map<List<WorkExperienceDTO>>(list);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;
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



        [HttpGet("{id}")]
        public async Task<IActionResult> GetActive(int id)
        {
            var model = await _workExperienceRepository.GetAsync(x => x.Id == id && x.IsActive);
            if (model == null)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.WORK_EXPERIENCE) };
                return BadRequest(_response);
            }

            _response.StatusCode = HttpStatusCode.Created;
            _response.Result = _mapper.Map<WorkExperienceDTO>(model);
            _response.IsSuccess = true;
            return Ok(_response);
        }

        [HttpPost]
        //[Authorize]
        public async Task<IActionResult> CreateAsync(WorkExperienceCreateDTO createDTO)
        {
            if (!ModelState.IsValid)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.WORK_EXPERIENCE) };
                return BadRequest(_response);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var newModel = _mapper.Map<WorkExperience>(createDTO);

            newModel.SubmiterId = userId;
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

        [HttpPut("changeStatus/{id}")]
        //[Authorize(Policy = "UpdateTutorPolicy")]
        public async Task<IActionResult> ApproveOrRejectRequest(ChangeStatusDTO changeStatusDTO)
        {
            try
            {
                var userId = _userRepository.GetAsync(x => x.Email == SD.ADMIN_EMAIL_DEFAULT).GetAwaiter().GetResult().Id;
                //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                //if (string.IsNullOrEmpty(userId))
                //{
                //    _response.StatusCode = HttpStatusCode.BadRequest;
                //    _response.IsSuccess = false;
                //    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                //    return BadRequest(_response);
                //}

                WorkExperience model = await _workExperienceRepository.GetAsync(x => x.Id == changeStatusDTO.Id, false, null, null);
                var tutor = await _userRepository.GetAsync(x => x.Id == model.SubmiterId);
                if (model == null || model.RequestStatus != Status.PENDING)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.WORK_EXPERIENCE) };
                    return BadRequest(_response);
                }
                if (changeStatusDTO.StatusChange == (int)Status.APPROVE)
                {
                    model.RequestStatus = Status.APPROVE;
                    model.UpdatedDate = DateTime.Now;
                    model.IsActive = true;
                    model.ApprovedId = userId;
                    await _workExperienceRepository.DeactivatePreviousVersionsAsync(model.OriginalId);
                    await _workExperienceRepository.UpdateAsync(model);

                    // Send mail
                    var subject = "Yêu cập nhật kinh nghiệm làm việc của bạn đã được chấp nhận!";
                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ChangeStatusTemplate.cshtml");
                    var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);
                    var htmlMessage = templateContent
                        .Replace("@Model.FullName", tutor.FullName)
                        .Replace("@Model.IssueName", $"Yêu cầu cập nhật kinh nghiệm làm việc của bạn tại {model.CompanyName}")
                        .Replace("@Model.IsApproved", Status.APPROVE.ToString());
                    _messageBus.SendMessage(new EmailLogger()
                    {
                        Email = tutor.Email,
                        Subject = subject,
                        Message = htmlMessage
                    }, queueName);


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

                    //Send mail
                    var subject = "Yêu cập nhật kinh nghiệm làm việc của bạn đã bị từ chối!";
                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ChangeStatusTemplate.cshtml");
                    var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);
                    var htmlMessage = templateContent
                        .Replace("@Model.FullName", tutor.FullName)
                        .Replace("@Model.IssueName", $"Yêu cầu cập nhật kinh nghiệm làm việc của bạn tại {model.CompanyName}")
                        .Replace("@Model.IsApproved", Status.REJECT.ToString())
                        .Replace("@Model.RejectionReason", changeStatusDTO.RejectionReason);
                    _messageBus.SendMessage(new EmailLogger()
                    {
                        Email = tutor.Email,
                        Subject = subject,
                        Message = htmlMessage
                    }, queueName);

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
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
