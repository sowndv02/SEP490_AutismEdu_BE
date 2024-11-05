using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.RabbitMQSender;
using backend_api.Repository;
using backend_api.Repository.IRepository;
using backend_api.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Net;

namespace backend_api.Controllers
{
    [Route("api/v{version:apiVersion}/test")]
    [ApiController]
    [ApiVersionNeutral]
    [Authorize]
    public class AccessCheckerController : ControllerBase
    {
        protected APIResponse _response;
        private readonly ILogger<AccessCheckerController> _logger;
        private IRabbitMQMessageSender _messageBus;
        private IConfiguration _configuration;
        private readonly IHubContext<NotificationHub> _hubContext;
        private IUserRepository _userRepository;
        public AccessCheckerController(ILogger<AccessCheckerController> logger, IRabbitMQMessageSender messageBus, 
            IConfiguration configuration, IHubContext<NotificationHub> hubContext, IUserRepository userRepository)
        {
            _messageBus = messageBus;
            _logger = logger;
            _response = new();
            _configuration = configuration;
            _hubContext = hubContext;
            _userRepository = userRepository;   
        }



        [HttpGet("SendNotification")]
        [AllowAnonymous]
        public async Task<object> SendNotificationAsync()
        {
            try
            {
                string userId = "";

                // TODO: Tạo 1 bảng lưu notification
                // Trả về total record chưa đọc
                // API: Đánh dấu đã đọc
                // API Đánh dấu all

                var connectionId = NotificationHub.GetConnectionIdByEmail(SD.ADMIN_EMAIL_DEFAULT);
                if (!string.IsNullOrEmpty(connectionId))
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync($"Notifications-{userId}", "Notification Test Message");
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    return Ok(_response);
                }
                else
                {
                    Console.WriteLine("No active connection found for the admin.");
                    _response.ErrorMessages = new List<string>()
                    {
                        "No active connection found for the admin."
                    };
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    return NotFound(_response);
                }
            }
            catch (Exception ex)
            {
                _response.ErrorMessages = new List<string>() { ex.Message };
                _response.IsSuccess = false;
            }
            return _response;
        }


        [HttpGet("SendMailRabbitMQ")]
        [AllowAnonymous]
        public object SendMailRabbitMQ()
        {
            try
            {
                _messageBus.SendMessage(new EmailLogger() { Email = "daoson03112002@gmail.com", Message = "Hello vietnam", Subject= "Test RabbitMQ"}, _configuration.GetValue<string>("RabbitMQSettings:QueueName"));
                _response.Result = true;
            }
            catch (Exception ex)
            {
                _response.ErrorMessages = new List<string>() { ex.Message };
                _response.IsSuccess = false;
            }
            return _response;
        }

        //Anyone can access this
        [HttpGet("all-access")]
        [AllowAnonymous]
        public IActionResult AllAccess()
        {
            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            return Ok(_response);
        }


        // Anyone that has logged in can access
        [HttpGet("authorized-access")]
        public IActionResult AuthorizedAccess()
        {
            try
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized access attempt by {User}", User.Identity?.Name ?? "Anonymous");
                _response.ErrorMessages = new List<string> { ex.Message };
                _response.StatusCode = HttpStatusCode.Unauthorized;
                _response.IsSuccess = false;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.ErrorMessages = new List<string> { ex.Message };
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                return Ok(_response);
            }

        }

        [HttpGet("user-or-admin-role-access")]
        [Authorize(Roles = $"{SD.ADMIN_ROLE},{SD.USER_ROLE}")]
        //account with role of user or admin can access
        public IActionResult UserORAdminRoleAccess()
        {
            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            return Ok(_response);
        }

        [HttpGet("admin-and-user")]
        [Authorize(Policy = "AdminAndUser")]
        //account with role of user or admin can access
        public IActionResult UserANDAdminRoleAccess()
        {
            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            return Ok(_response);
        }

        [HttpGet("admin")]
        [Authorize(Policy = "Admin")]
        //account with role of admin can access
        public IActionResult AdminRoleAccess()
        {
            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            return Ok(_response);
        }

        [HttpGet("admin-role-create-claim")]
        [Authorize(Policy = "AdminRole_CreateClaim")]
        //account with admin role and create Claim can access
        public IActionResult Admin_CreateAccess()
        {
            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            return Ok(_response);
        }

        [HttpGet("admin-role-create-edit-delete-claim")]
        [Authorize(Policy = "AdminRole_CreateEditDeleteClaim")]
        //account with admin role and (create & Edit & Delete) Claim can access (AND NOT OR)
        public IActionResult Admin_Create_Edit_DeleteAccess()
        {
            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            return Ok(_response);
        }

        [HttpGet("admin-role-create-edit-delete-claim-or-supperadmin")]
        [Authorize(Policy = "AdminRole_CreateEditDeleteClaim_ORSuperAdminRole")]
        //account with admin role and (create & Edit & Delete) Claim can access (AND NOT OR)
        public IActionResult Admin_Create_Edit_DeleteAccess_OR_SuperAdminRole()
        {
            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            return Ok(_response);
        }

        [HttpGet("admin-with-more-than-1000days")]
        [Authorize(Policy = "AdminWithMoreThan1000Days")]
        public IActionResult OnlyBhrugen()
        {
            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            return Ok(_response);
        }

        [HttpGet("first-name-auth")]
        [Authorize(Policy = "FirstNameAuth")]
        public IActionResult FirstNameAuth()
        {
            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            return Ok(_response);
        }


        [HttpGet("assign-role")]
        [Authorize(Policy = "AssignRolePolicy")]
        public IActionResult AssignRoleAccess()
        {
            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            return Ok(_response);
        }

        [HttpGet("assign-claim")]
        [Authorize(Policy = "AssignClaimPolicy")]
        public IActionResult AssignClaimAccess()
        {
            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            return Ok(_response);
        }

    }
}
