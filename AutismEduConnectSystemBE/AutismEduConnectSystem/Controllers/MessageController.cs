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

        public MessageController(IConversationRepository conversationRepository,
            IMapper mapper, IResourceService resourceService,
            ILogger<MessageController> logger, IUserRepository userRepository, 
            IMessageRepository messageRepository)
        {
            _userRepository = userRepository;
            _response = new APIResponse();
            _mapper = mapper;
            _conversationRepository = conversationRepository;
            _resourceService = resourceService;
            _logger = logger;
            _messageRepository = messageRepository;
        }

        [HttpPost]
        [Authorize(Roles = $"{SD.TUTOR_ROLE},{SD.PARENT_ROLE}")]
        public async Task<ActionResult<APIResponse>> CreateAsync(ConverstationCreateDTO createDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model state is invalid. Returning BadRequest.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.CONVERSATION) };
                    return BadRequest(_response);
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var newConversation = new Conversation();
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
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
                var returnModel = await _conversationRepository.GetAsync(x => x.Id == result.Id, false, "Parent,Tutor", null);

                _response.StatusCode = HttpStatusCode.Created;
                _response.Result = _mapper.Map<ConversationDTO>(result);
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
