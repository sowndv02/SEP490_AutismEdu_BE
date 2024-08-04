using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend_api.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    [ApiVersionNeutral]
    public class AccessCheckerController : ControllerBase
    {
        //Anyone can access this
        [AllowAnonymous]
        public IActionResult AllAccess()
        {
            return Ok();
        }


        //Anyone that has logged in can access
        public IActionResult AuthorizedAccess()
        {
            return Ok();
        }

        [Authorize(Roles = $"{SD.Admin},{SD.User}")]
        //account with role of user or admin can access
        public IActionResult UserORAdminRoleAccess()
        {
            return Ok();
        }

        [Authorize(Policy = "AdminAndUser")]
        //account with role of user or admin can access
        public IActionResult UserANDAdminRoleAccess()
        {
            return Ok();
        }

        [Authorize(Policy = "Admin")]
        //account with role of admin can access
        public IActionResult AdminRoleAccess()
        {
            return Ok();
        }

        [Authorize(Policy = "AdminRole_CreateClaim")]
        //account with admin role and create Claim can access
        public IActionResult Admin_CreateAccess()
        {
            return Ok();
        }

        [Authorize(Policy = "AdminRole_CreateEditDeleteClaim")]
        //account with admin role and (create & Edit & Delete) Claim can access (AND NOT OR)
        public IActionResult Admin_Create_Edit_DeleteAccess()
        {
            return Ok();
        }

        [Authorize(Policy = "AdminRole_CreateEditDeleteClaim_ORSuperAdminRole")]
        //account with admin role and (create & Edit & Delete) Claim can access (AND NOT OR)
        public IActionResult Admin_Create_Edit_DeleteAccess_OR_SuperAdminRole()
        {
            return Ok();
        }

        [Authorize(Policy = "AdminWithMoreThan1000Days")]
        public IActionResult OnlyBhrugen()
        {
            return Ok();
        }

        [Authorize(Policy = "FirstNameAuth")]
        public IActionResult FirstNameAuth()
        {
            return Ok();
        }
    }
}
