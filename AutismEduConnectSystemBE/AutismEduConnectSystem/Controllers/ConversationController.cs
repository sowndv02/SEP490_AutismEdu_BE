﻿using AutismEduConnectSystem.DTOs;
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
    public class ConversationController : ControllerBase
    {
        private readonly IConversationRepository _conversationRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        private readonly ILogger<ConversationController> _logger;
        private readonly IResourceService _resourceService;

        public ConversationController(IConversationRepository conversationRepository,
            IMapper mapper, IResourceService resourceService,
            ILogger<ConversationController> logger, IUserRepository userRepository,
            IMessageRepository messageRepository, IHubContext<NotificationHub> hubContext,
            IConfiguration configuration)
        {
            _hubContext = hubContext;
            _messageRepository = messageRepository;
            _userRepository = userRepository;
            _response = new APIResponse();
            _mapper = mapper;
            _conversationRepository = conversationRepository;
            _resourceService = resourceService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Roles = $"{SD.TUTOR_ROLE},{SD.PARENT_ROLE}")]
        public async Task<ActionResult<APIResponse>> CreateAsync(ConverstationCreateDTO createDTO)
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
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.CONVERSATION) };
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

                var newConversation = new Conversation();

                if (userRoles != null && (userRoles.Contains(SD.TUTOR_ROLE)))
                {
                    newConversation.TutorId = userId;
                    newConversation.ParentId = createDTO.ReceiverId;
                }
                else
                {
                    newConversation.ParentId = userId;
                    newConversation.TutorId = createDTO.ReceiverId;
                }

                var result = await _conversationRepository.CreateAsync(newConversation);
                var message = await _messageRepository.CreateAsync(new Message()
                {
                    Content = createDTO.Message,
                    SenderId = userId,
                    IsRead = false,
                    ConversationId = result.Id,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                });
                var returnModel = await _messageRepository.GetAsync(x => x.Id == message.Id, false, "Sender,Conversation", null);

                returnModel.Conversation.Parent = await _userRepository.GetAsync(x => x.Id == returnModel.Conversation.ParentId);
                if (returnModel.Conversation.Tutor == null)
                {
                    returnModel.Conversation.Tutor = new Tutor();
                }
                returnModel.Conversation.Tutor.User = await _userRepository.GetAsync(x => x.Id == returnModel.Conversation.TutorId);
                //SignalR
                var connectionId = NotificationHub.GetConnectionIdByUserId(createDTO.ReceiverId);
                if (!string.IsNullOrEmpty(connectionId))
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync($"Messages-{createDTO.ReceiverId}", _mapper.Map<MessageDTO>(returnModel));
                }

                _response.StatusCode = HttpStatusCode.Created;
                _response.Result = _mapper.Map<ConversationDTO>(result);
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpGet]
        [Authorize(Roles = $"{SD.TUTOR_ROLE},{SD.PARENT_ROLE}")]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] int pageNumber = 1, int pageSize = 10)
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

                int totalCount = 0;
                List<Conversation> result = new();

                if (userRoles != null && userRoles.Contains(SD.TUTOR_ROLE))
                {
                    var (countTutorConversation, listTutorConversation) = await _conversationRepository.GetAllNotPagingAsync(x => x.TutorId == userId, "Parent", null, x => x.UpdatedDate, true);
                    totalCount = countTutorConversation;
                    result = listTutorConversation;
                }
                else if (userRoles != null && userRoles.Contains(SD.PARENT_ROLE))
                {
                    var (countParentConversation, listParentConversation) = await _conversationRepository.GetAllNotPagingAsync(x => x.ParentId == userId, "Tutor", null, x => x.UpdatedDate, true);
                    totalCount = countParentConversation;
                    result = listParentConversation;
                }
                foreach (var conversation in result)
                {
                    if (conversation.Tutor != null)
                    {
                        conversation.Tutor.User = await _userRepository.GetAsync(x => x.Id == conversation.TutorId, false, null);
                    }
                    var (countMessages, listMessages) = await _messageRepository.GetAllAsync(x => x.ConversationId == conversation.Id, "Sender", pageSize: 1, pageNumber: 1, x => x.CreatedDate, true);
                    conversation.Messages = listMessages;
                }

                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = totalCount };
                _response.IsSuccess = true;
                var sortedConversations = result
                .OrderByDescending(conversation => conversation.Messages
                    .Max(message => (DateTime?)message.CreatedDate) ?? DateTime.MinValue)
                .ToList();
                result = sortedConversations.ToList();
                var resultResponse = _mapper.Map<List<ConversationDTO>>(result);
                if (userRoles != null && userRoles.Contains(SD.TUTOR_ROLE))
                {
                    foreach (var item in resultResponse)
                    {
                        item.User = _mapper.Map<ApplicationUserDTO>(result.FirstOrDefault(u => u.Id == item.Id).Parent);
                    }
                }
                else if (userRoles != null && userRoles.Contains(SD.PARENT_ROLE))
                {
                    foreach (var item in resultResponse)
                    {
                        item.User = _mapper.Map<ApplicationUserDTO>(result.FirstOrDefault(u => u.Id == item.Id).Tutor.User);
                    }
                }

                _response.Result = resultResponse;
                _response.Pagination = pagination;
                _response.StatusCode = HttpStatusCode.OK;
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
