using backend_api.Authorize.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace backend_api.Authorize
{
    public class AssignClaimHandler : AuthorizationHandler<AssignClaimRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AssignClaimRequirement requirement)
        {
            if (context.User.HasClaim(c => c.Type == requirement.ClaimType && c.Value == requirement.ClaimValue))
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }
}
