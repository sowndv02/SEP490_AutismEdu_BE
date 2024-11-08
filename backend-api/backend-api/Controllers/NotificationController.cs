using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs.UpdateDTOs;
using backend_api.Models.DTOs;
using backend_api.Repository;
using backend_api.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using backend_api.Services.IServices;
using System.Security.Claims;
using System.Linq.Expressions;

namespace backend_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiVersionNeutral]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        private readonly ILogger<NotificationController> _logger;
        private readonly IResourceService _resourceService;
        protected int pageSize = 0;

        public NotificationController(INotificationRepository notificationRepository,
            IMapper mapper, IResourceService resourceService, IConfiguration configuration,
            ILogger<NotificationController> logger)
        {
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _response = new APIResponse();
            _mapper = mapper;
            _notificationRepository = notificationRepository;
            _logger = logger;
            _resourceService = resourceService;
        }

        [HttpPut("read/{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateStatus(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                Notification model = await _notificationRepository.GetAsync(x => x.Id == id && x.ReceiverId == userId, true, null, null);
                if (model == null)
                {
                    _logger.LogWarning("Notification with ID: {Id} is either not found.", id);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.NOTIFICATION) };
                    return BadRequest(_response);
                }
                model.IsRead = true;
                model.UpdatedDate = DateTime.Now;
                var result = await _notificationRepository.UpdateAsync(model);
                _response.Result = _mapper.Map<NotificationDTO>(result);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while processing the status change for package payment ID: {Id}. Error: {Error}", id, ex.Message);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPut("ReadAll")]
        [Authorize]
        public async Task<IActionResult> ReadAllNotification()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var (total, list) = await _notificationRepository.GetAllNotPagingAsync(x => x.ReceiverId == userId, null, null, x => x.CreatedDate, true);
                foreach (var item in list) 
                {
                    item.IsRead = true;
                    item.UpdatedDate = DateTime.Now;
                    await _notificationRepository.UpdateAsync(item);
                }
                _response.Result = _mapper.Map<List<NotificationDTO>>(list);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while processing the status change for notification. Error: {Error}", ex.Message);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllAsync(int pageNumber = 1)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var (total, list) = await _notificationRepository.GetAllAsync(x => x.ReceiverId == userId, null, pageSize, pageNumber, x => x.CreatedDate, true);
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = total };
                _response.IsSuccess = true;
                _response.Pagination = pagination;
                _response.Result = _mapper.Map<List<NotificationDTO>>(list);
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
