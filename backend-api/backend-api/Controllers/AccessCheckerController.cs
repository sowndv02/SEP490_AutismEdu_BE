using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend_api.Controllers
{
    [Route("api/v{version:apiVersion}/test")]
    [ApiController]
    [ApiVersionNeutral]
    [Authorize]
    public class AccessCheckerController : ControllerBase
    {
        public AccessCheckerController()
        {
            
        }

        //Anyone can access this
        [HttpGet("all-access")]
        [AllowAnonymous]
        public IActionResult AllAccess()
        {
            return Ok();
        }


        // Anyone that has logged in can access
        [HttpGet("authorized-access")]
        public IActionResult AuthorizedAccess()
        {
            return Ok();
        }

        [HttpGet("user-or-admin-role-access")]
        [Authorize(Roles = $"{SD.Admin},{SD.User}")]
        //account with role of user or admin can access
        public IActionResult UserORAdminRoleAccess()
        {
            return Ok();
        }

        [HttpGet("admin-and-user")]
        [Authorize(Policy = "AdminAndUser")]
        //account with role of user or admin can access
        public IActionResult UserANDAdminRoleAccess()
        {
            return Ok();
        }

        [HttpGet("admin")]
        [Authorize(Policy = "Admin")]
        //account with role of admin can access
        public IActionResult AdminRoleAccess()
        {
            return Ok();
        }

        [HttpGet("admin-role-create-claim")]
        [Authorize(Policy = "AdminRole_CreateClaim")]
        //account with admin role and create Claim can access
        public IActionResult Admin_CreateAccess()
        {
            return Ok();
        }

        [HttpGet("admin-role-create-edit-delete-claim")]
        [Authorize(Policy = "AdminRole_CreateEditDeleteClaim")]
        //account with admin role and (create & Edit & Delete) Claim can access (AND NOT OR)
        public IActionResult Admin_Create_Edit_DeleteAccess()
        {
            return Ok();
        }

        [HttpGet("admin-role-create-edit-delete-claim-or-supperadmin")]
        [Authorize(Policy = "AdminRole_CreateEditDeleteClaim_ORSuperAdminRole")]
        //account with admin role and (create & Edit & Delete) Claim can access (AND NOT OR)
        public IActionResult Admin_Create_Edit_DeleteAccess_OR_SuperAdminRole()
        {
            return Ok();
        }

        [HttpGet("admin-with-more-than-1000days")]
        [Authorize(Policy = "AdminWithMoreThan1000Days")]
        public IActionResult OnlyBhrugen()
        {
            return Ok();
        }

        [HttpGet("first-name-auth")]
        [Authorize(Policy = "FirstNameAuth")]
        public IActionResult FirstNameAuth()
        {
            return Ok();
        }


        [HttpGet("assign-role")]
        [Authorize(Policy = "AssignRolePolicy")]
        public IActionResult AssignRoleAccess()
        {
            return Ok();
        }

        [HttpGet("assign-claim")]
        [Authorize(Policy = "AssignClaimPolicy")]
        public IActionResult AssignClaimAccess()
        {
            return Ok();
        }

    }
}
