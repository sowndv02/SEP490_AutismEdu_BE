using AutoMapper;
using AutismEduConnectSystem.Models.DTOs.CreateDTOs;
using AutismEduConnectSystem.Models.DTOs;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.SignalR;
using Microsoft.AspNetCore.SignalR;
using AutismEduConnectSystem.Repository;

namespace AutismEduConnectSystem.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersionNeutral]
    public class MessageController : ControllerBase
    {

        private readonly IConversationRepository _conversationRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        private readonly ILogger<MessageController> _logger;
        private readonly IResourceService _resourceService;
        private readonly IAttachmentRepository _attachmentRepository;
        private readonly IHubContext<NotificationHub> _hubContext;
        protected int pageSize = 0;
        

        public MessageController(IConversationRepository conversationRepository,
            IMapper mapper, IResourceService resourceService,
            ILogger<MessageController> logger, IUserRepository userRepository, 
            IMessageRepository messageRepository, IHubContext<NotificationHub> hubContext,
            IConfiguration configuration, IAttachmentRepository attachmentRepository)
        {
            _attachmentRepository = attachmentRepository;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _hubContext = hubContext;
            _userRepository = userRepository;
            _response = new APIResponse();
            _mapper = mapper;
            _conversationRepository = conversationRepository;
            _resourceService = resourceService;
            _logger = logger;
            _messageRepository = messageRepository;
        }

        [HttpPut("read/{id}")]
        [Authorize(Roles = $"{SD.TUTOR_ROLE},{SD.PARENT_ROLE}")]
        public async Task<IActionResult> UpdateStatus(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                Message model = await _messageRepository.GetAsync(x => x.Id == id, true, null, null);
                if (model == null)
                {
                    _logger.LogWarning("Message with ID: {Id} is either not found.", id);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.MESSAGE) };
                    return BadRequest(_response);
                }
                model.IsRead = true;
                model.UpdatedDate = DateTime.Now;
                var result = await _messageRepository.UpdateAsync(model);
                var (count, list) = await _messageRepository.GetAllNotPagingAsync(x => x.ConversationId == model.ConversationId && x.CreatedDate <= model.CreatedDate && !x.IsRead, null, null, x => x.CreatedDate, true);

                foreach (var message in list) 
                {
                    message.IsRead = true;
                    message.UpdatedDate = DateTime.Now;
                    await _messageRepository.UpdateAsync(message);
                }
                _response.Result = _mapper.Map<MessageDTO>(result);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while processing the status change for message ID: {Id}. Error: {Error}", id, ex.Message);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet("{conversationId}")]
        [Authorize(Roles = $"{SD.TUTOR_ROLE},{SD.PARENT_ROLE}")]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromRoute] int conversationId, [FromQuery] int pageNumber = 1)
        {
            try
            {
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                Conversation conversation = null;
                if (userRoles != null && userRoles.Contains(SD.TUTOR_ROLE))
                {
                    conversation = await _conversationRepository.GetAsync(x => x.Id == conversationId && x.TutorId == userId, false, null, null);
                }
                else if (userRoles != null && userRoles.Contains(SD.PARENT_ROLE))
                {
                    conversation = await _conversationRepository.GetAsync(x => x.Id == conversationId && x.ParentId == userId, false, null, null);
                }
                if(conversation == null)
                {
                    _logger.LogWarning("Cannot access to message. Returning BadRequest.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.MESSAGE) };
                    return BadRequest(_response);
                }
                var (countMessages, messages) = await _messageRepository.GetAllAsync(x => x.ConversationId == conversationId, "Sender,Conversation", pageSize, pageNumber, x => x.CreatedDate, true);
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = countMessages };
                _response.IsSuccess = true;
                _response.Result = _mapper.Map<List<MessageDTO>>(messages);
                _response.Pagination = pagination;
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


        [HttpPost]
        [Authorize(Roles = $"{SD.TUTOR_ROLE},{SD.PARENT_ROLE}")]
        public async Task<ActionResult<APIResponse>> CreateAsync(MessageCreateDTO createDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model state is invalid. Returning BadRequest.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.MESSAGE) };
                    return BadRequest(_response);
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var newMessage = new Message()
                {
                    ConversationId = createDTO.ConversationId,
                    Content = createDTO.Content,
                    IsRead = false,
                    SenderId = userId,
                    CreatedDate = DateTime.Now
                };
                

                var result = await _messageRepository.CreateAsync(newMessage);

                var conversation = await _conversationRepository.GetAsync(x => x.Id == createDTO.ConversationId, false, null, null);
                var receiverId = string.Empty;
                var returnModel = await _messageRepository.GetAsync(x => x.Id == result.Id, false, "Sender,Conversation", null);
                if (conversation != null) 
                {
                    if (conversation.TutorId.Equals(userId)) 
                        receiverId = conversation.ParentId;
                    else receiverId = conversation.TutorId;

                }

                //SignalR
                var connectionId = NotificationHub.GetConnectionIdByUserId(receiverId);
                if (!string.IsNullOrEmpty(connectionId))
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync($"Messages-{receiverId}", _mapper.Map<MessageDTO>(returnModel));
                }

                _response.StatusCode = HttpStatusCode.Created;
                _response.Result = _mapper.Map<MessageDTO>(returnModel);
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while creating conversation: {ex.Message}");
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

    }
}
