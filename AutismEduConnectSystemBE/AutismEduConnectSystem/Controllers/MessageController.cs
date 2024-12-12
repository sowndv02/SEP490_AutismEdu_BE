using AutismEduConnectSystem.DTOs;
using AutismEduConnectSystem.DTOs.CreateDTOs;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.SignalR;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Net;
using System.Security.Claims;

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
        public async Task<ActionResult<APIResponse>> UpdateStatus(int id)
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
                if (userRoles == null || (!userRoles.Contains(SD.TUTOR_ROLE) && !userRoles.Contains(SD.PARENT_ROLE)))
                {
                   
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }

                Conversation model = await _conversationRepository.GetAsync(x => x.Id == id, true, null, null);
                if (model == null)
                {
                    _logger.LogWarning("Message with ID: {Id} is either not found.", id);
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.MESSAGE) };
                    return NotFound(_response);
                }

                var (count, list) = await _messageRepository.GetAllNotPagingAsync(x => x.ConversationId == model.Id && !x.IsRead, null, null, x => x.CreatedDate, true);

                foreach (var message in list)
                {
                    if (message.SenderId != userId)
                    {
                        message.IsRead = true;
                        message.UpdatedDate = DateTime.Now;
                        await _messageRepository.UpdateAsync(message);
                    }
                }
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
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromRoute] int conversationId, [FromQuery] int pageNumber = 0)
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
                if (userRoles == null || (!userRoles.Contains(SD.TUTOR_ROLE) && !userRoles.Contains(SD.PARENT_ROLE)))
                {
                   
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }

                Conversation conversation = null;
                if (userRoles != null && userRoles.Contains(SD.TUTOR_ROLE))
                {
                    conversation = await _conversationRepository.GetAsync(x => x.Id == conversationId && x.TutorId == userId, false, null, null);
                }
                else if (userRoles != null && userRoles.Contains(SD.PARENT_ROLE))
                {
                    conversation = await _conversationRepository.GetAsync(x => x.Id == conversationId && x.ParentId == userId, false, null, null);
                }
                if (conversation == null)
                {
                    _logger.LogWarning("Cannot access to message. Returning BadRequest.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.MESSAGE) };
                    return BadRequest(_response);
                }
                var messages = new List<Message>();
                int totalMessage = 0;
                if(pageSize == 0 || pageNumber == 0)
                {
                    var (countNotPagingMessages, messagesNotPaging) = await _messageRepository.GetAllNotPagingAsync(x => x.ConversationId == conversationId, "Sender,Conversation", null, x => x.CreatedDate, true);
                    totalMessage = countNotPagingMessages;
                    messages = messagesNotPaging;
                }else
                {
                    var (countMessagesPaging, messagesPaging) = await _messageRepository.GetAllAsync(x => x.ConversationId == conversationId, "Sender,Conversation", pageSize, pageNumber, x => x.CreatedDate, true);
                    totalMessage = countMessagesPaging;
                    messages = messagesPaging;
                }
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = totalMessage };
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
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model state is invalid. Returning BadRequest.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.MESSAGE) };
                    return BadRequest(_response);
                }
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                if (userRoles == null || (!userRoles.Contains(SD.TUTOR_ROLE) && !userRoles.Contains(SD.PARENT_ROLE)))
                {
                   
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }

                var newMessage = new Message()
                {
                    ConversationId = createDTO.ConversationId,
                    Content = createDTO.Content,
                    IsRead = false,
                    SenderId = userId,
                    CreatedDate = DateTime.Now
                };


                var result = await _messageRepository.CreateAsync(newMessage);

                var conversation = await _conversationRepository.GetAsync(x => x.Id == createDTO.ConversationId, true, null, null);
                var receiverId = string.Empty;
                var returnModel = await _messageRepository.GetAsync(x => x.Id == result.Id, false, "Sender,Conversation", null);
                if (conversation != null)
                {
                    if (conversation.TutorId.Equals(userId))
                        receiverId = conversation.ParentId;
                    else receiverId = conversation.TutorId;

                    conversation.UpdatedDate = DateTime.Now;
                    await _conversationRepository.UpdateAsync(conversation);
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
