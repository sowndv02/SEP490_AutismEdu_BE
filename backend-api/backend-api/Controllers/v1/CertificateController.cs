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
    public class CertificateController : ControllerBase
    {

        private readonly IUserRepository _userRepository;
        private readonly ICertificateRepository _certificateRepository;
        private readonly ICertificateMediaRepository _certificateMediaRepository;
        private readonly ICurriculumRepository _curriculumRepository;
        private readonly IWorkExperienceRepository _workExperienceRepository;
        private readonly IBlobStorageRepository _blobStorageRepository;
        private readonly ILogger<CertificateController> _logger;
        private readonly IMapper _mapper;
        private readonly IRabbitMQMessageSender _messageBus;
        private string queueName = string.Empty;
        protected APIResponse _response;
        protected int pageSize = 0;
        private readonly IResourceService _resourceService;


        public CertificateController(IUserRepository userRepository, ICertificateRepository certificateRepository,
            ILogger<CertificateController> logger, IBlobStorageRepository blobStorageRepository,
            IMapper mapper, IConfiguration configuration, 
            ICertificateMediaRepository certificateMediaRepository, ICurriculumRepository curriculumRepository,
            IWorkExperienceRepository workExperienceRepository, IRabbitMQMessageSender messageBus, IResourceService resourceService)
        {
            _resourceService = resourceService;
            _workExperienceRepository = workExperienceRepository;
            _curriculumRepository = curriculumRepository;
            _certificateMediaRepository = certificateMediaRepository;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            queueName = configuration["RabbitMQSettings:QueueName"];
            _response = new APIResponse();
            _mapper = mapper;
            _blobStorageRepository = blobStorageRepository;
            _logger = logger;
            _userRepository = userRepository;
            _certificateRepository = certificateRepository;
            _messageBus = messageBus;
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetActive(int id)
        {
            try
            {
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

                var result = new Certificate();
                if (userRoles != null && userRoles.Contains(SD.TUTOR_ROLE))
                {
                    result = await _certificateRepository.GetAsync(x => x.Id == id && !x.IsDeleted, false, "CertificateMedias", null);
                }
                else
                {
                    result = await _certificateRepository.GetAsync(x => x.Id == id, false, "CertificateMedias", null);
                }

                if (result == null)
                {
                    _logger.LogError("Certificate with ID {Id} not found or is deleted.", id);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.CERTIFICATE) };
                    return BadRequest(_response);
                }
                _response.StatusCode = HttpStatusCode.Created;
                _response.Result = _mapper.Map<CertificateDTO>(result);
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "An error occurred while get a certificate {Id}", id);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            } 
        }

        [HttpPost]
        [Authorize(SD.TUTOR_ROLE)]
        public async Task<IActionResult> CreateAsync([FromForm] CertificateCreateDTO createDTO)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model state is invalid for user {UserId}", userId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.CERTIFICATE) };
                    return BadRequest(_response);
                }

                var newModel = _mapper.Map<Certificate>(createDTO);

                newModel.SubmiterId = userId;
                var certificate = await _certificateRepository.CreateAsync(newModel);
                foreach (var media in createDTO.Medias)
                {
                    using var stream = media.OpenReadStream();
                    var url = await _blobStorageRepository.Upload(stream, string.Concat(Guid.NewGuid().ToString(), Path.GetExtension(media.FileName)));
                    var objMedia = new CertificateMedia() { CertificateId = certificate.Id, UrlPath = url, CreatedDate = DateTime.Now };
                    await _certificateMediaRepository.CreateAsync(objMedia);
                }
                // TODO: Add log
                _response.StatusCode = HttpStatusCode.Created;
                _response.Result = _mapper.Map<CertificateDTO>(newModel);
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating a certificate for user {UserId}", userId);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPut("changeStatus/{id}")]
        [Authorize(Roles = SD.STAFF_ROLE)]
        public async Task<IActionResult> ApproveOrRejectRequest(ChangeStatusDTO changeStatusDTO)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            try
            {
                Certificate model = await _certificateRepository.GetAsync(x => x.Id == changeStatusDTO.Id, false, "CertificateMedias", null);
                var tutor = await _userRepository.GetAsync(x => x.Id == model.SubmiterId);
                if (model == null || model.RequestStatus != Status.PENDING)
                {
                    _logger.LogWarning("Invalid request status or certificate not found for certificate ID {CertificateId} by user {UserId}", changeStatusDTO.Id, userId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.CERTIFICATE) };
                    return BadRequest(_response);
                }
                if (changeStatusDTO.StatusChange == (int)Status.APPROVE)
                {
                    model.RequestStatus = Status.APPROVE;
                    model.UpdatedDate = DateTime.Now;
                    model.ApprovedId = userId;
                    await _certificateRepository.UpdateAsync(model);

                    // Send mail
                    var subject = "Yêu cập nhật chứng chỉ của bạn đã được chấp nhận!";
                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ChangeStatusTemplate.cshtml");
                    if (System.IO.File.Exists(templatePath))
                    {
                        var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);
                        var htmlMessage = templateContent
                        .Replace("@Model.FullName", tutor.FullName)
                        .Replace("@Model.IssueName", $"Yêu cầu cập nhật chứng chỉ {model.CertificateName} của bạn")
                        .Replace("@Model.IsApproved", Status.APPROVE.ToString());

                        _messageBus.SendMessage(new EmailLogger()
                        {
                            Email = tutor.Email,
                            Subject = subject,
                            Message = htmlMessage
                        }, queueName);
                    }

                    _response.Result = _mapper.Map<CertificateDTO>(model);
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
                    await _certificateRepository.UpdateAsync(model);

                    //Send mail
                    var subject = "Yêu cập nhật chứng chỉ của bạn đã bị từ chối!";
                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ChangeStatusTemplate.cshtml");
                    if (System.IO.File.Exists(templatePath))
                    {
                        var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);
                        var htmlMessage = templateContent
                            .Replace("@Model.FullName", tutor.FullName)
                            .Replace("@Model.IssueName", $"Yêu cầu cập nhật chứng chỉ {model.CertificateName} của bạn")
                            .Replace("@Model.IsApproved", Status.REJECT.ToString())
                            .Replace("@Model.RejectionReason", changeStatusDTO.RejectionReason);
                        _messageBus.SendMessage(new EmailLogger()
                        {
                            Email = tutor.Email,
                            Subject = subject,
                            Message = htmlMessage
                        }, queueName);
                    }

                    _response.Result = _mapper.Map<CertificateDTO>(model);
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
                _logger.LogError(ex, "An error occurred while changing the status of certificate ID {CertificateId} by user {UserId}", changeStatusDTO.Id, userId);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpGet]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? search, string? status = SD.STATUS_ALL, string? orderBy = SD.CREADTED_DATE, string? sort = SD.ORDER_DESC, int pageNumber = 1)
        {
            try
            {
                int totalCount = 0;
                List<Certificate> list = new();
                Expression<Func<Certificate, bool>> filter = u => true;
                Expression<Func<Certificate, object>> orderByQuery = u => true;
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

                if (userRoles.Contains(SD.TUTOR_ROLE))
                {
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    filter = u => !string.IsNullOrEmpty(u.SubmiterId) && u.SubmiterId == userId && !u.IsDeleted;
                }
                if (search != null && !string.IsNullOrEmpty(search))
                {
                    filter = filter.AndAlso(x => x.CertificateName.ToLower().Contains(search.ToLower()));
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

                var (count, result) = await _certificateRepository.GetAllAsync(
                    filter,
                    "Submiter,CertificateMedias",
                    pageSize: 5,
                    pageNumber: pageNumber,
                    orderByQuery,
                    isDesc
                );

                list = result;
                totalCount = count;
                foreach (var item in list)
                {
                    item.Submiter.User = await _userRepository.GetAsync(x => x.Id == item.Submiter.TutorId, false, null);
                    var (total, curriculums) = await _curriculumRepository.GetAllNotPagingAsync(x => x.SubmiterId == item.Submiter.TutorId && x.IsActive, null, null);
                    item.Submiter.Curriculums = curriculums;
                    var (totalWorkExperience, workexperiences) = await _workExperienceRepository.GetAllNotPagingAsync(x => x.SubmiterId == item.Submiter.TutorId && x.IsActive, null, null);
                    item.Submiter.WorkExperiences = workexperiences;
                }
                // Setup pagination and response
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = 5, Total = totalCount };
                _response.Result = _mapper.Map<List<CertificateDTO>>(list);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetAllAsync Certifcate");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpDelete("{id:int}", Name = "DeleteCertificate")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> DeleteAsync(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            try
            {
                if (id == 0)
                {
                    _logger.LogWarning("Invalid curriculum ID: {CurriculumId}. Returning BadRequest.", id);
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                var model = await _certificateRepository.GetAsync(x => x.Id == id && x.SubmiterId == userId, false, null);

                if (model == null)
                {
                    _logger.LogWarning("Certificate not found for ID: {CertificateId} and User ID: {UserId}. Returning BadRequest.", id, userId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.CERTIFICATE) };
                    return BadRequest(_response);
                }
                model.IsDeleted = true;
                await _certificateRepository.UpdateAsync(model);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting certificate ID: {CertificateId}", id);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

    }
}
