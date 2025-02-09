﻿using AutismEduConnectSystem.Authorize.Requirements;
using AutismEduConnectSystem.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AutismEduConnectSystem.Authorize
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
