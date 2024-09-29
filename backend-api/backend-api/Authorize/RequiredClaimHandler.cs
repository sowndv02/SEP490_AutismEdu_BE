using backend_api.Authorize.Requirements;
using backend_api.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace backend_api.Authorize
{
    public class RequiredClaimHandler : AuthorizationHandler<RequiredClaimRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RequiredClaimRequirement requirement)
        {
            if (context.User.HasClaim(c => c.Type == requirement.ClaimType && c.Value == requirement.ClaimValue))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
