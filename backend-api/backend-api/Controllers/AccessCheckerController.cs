using backend_api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public AccessCheckerController(ILogger<AccessCheckerController> logger)
        {
            _logger = logger;
            _response = new();
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
