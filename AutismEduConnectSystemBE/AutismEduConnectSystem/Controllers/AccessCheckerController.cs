using AutismEduConnectSystem.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Net;

namespace AutismEduConnectSystem.Controllers
{
    [Route("api/v{version:apiVersion}/test")]
    [ApiController]
    [ApiVersionNeutral]
    public class AccessCheckerController : ControllerBase
    {
        protected APIResponse _response;
        private readonly ILogger<AccessCheckerController> _logger;
        private IEmailSender _messageBus;
        private IConfiguration _configuration;
        private INotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationHub> _hubContext;
        private IUserRepository _userRepository;
        public AccessCheckerController(ILogger<AccessCheckerController> logger, IEmailSender messageBus, 
            IConfiguration configuration, IHubContext<NotificationHub> hubContext, IUserRepository userRepository, 
            INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
            _messageBus = messageBus;
            _logger = logger;
            _response = new();
            _configuration = configuration;
            _hubContext = hubContext;
            _userRepository = userRepository;   
        }



        [HttpGet("SendNotification")]
        public async Task<object> SendNotificationAsync()
        {
            try
            {
                var userId = "f53e66ab-9374-4870-8ec5-ea145a30c16f";
                var notfication = new Notification()
                {
                    ReceiverId = userId,
                    Message = "Notification Test Message",
                    UrlDetail = string.Concat(SD.URL_FE, SD.URL_FE_TUTOR_TUTOR_REQUEST),
                    IsRead = false,
                    CreatedDate = DateTime.Now
                };
                var notificationResult = await _notificationRepository.CreateAsync(notfication);

                var connectionId = NotificationHub.GetConnectionIdByUserId("Notifications-{userId}");
                if (!string.IsNullOrEmpty(connectionId))
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync($"Notifications-{userId}", notificationResult);
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


        //[HttpGet("SendMailRabbitMQ")]
        //[AllowAnonymous]
        //public object SendMailRabbitMQ()
        //{
        //    try
        //    {
        //        _messageBus.SendMessage(new EmailLogger() { Email = "daoson03112002@gmail.com", Message = "Hello vietnam", Subject= "Test RabbitMQ"}, _configuration.GetValue<string>("RabbitMQSettings:QueueName"));
        //        _response.Result = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        _response.ErrorMessages = new List<string>() { ex.Message };
        //        _response.IsSuccess = false;
        //    }
        //    return _response;
        //}

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
        [Authorize(Roles = $"{SD.ADMIN_ROLE}")]
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
