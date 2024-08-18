using backend_api.Authorize.Requirements;
using backend_api.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace backend_api.Authorize
{
    public class AssignRoleHandler : AuthorizationHandler<AssignRoleRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AssignRoleRequirement requirement)
        {
            if (context.User.HasClaim(c => c.Type == requirement.ClaimType && c.Value == requirement.ClaimValue))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
