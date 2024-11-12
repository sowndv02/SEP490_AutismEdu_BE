using Microsoft.AspNetCore.Authorization;

namespace AutismEduConnectSystem.Authorize.Requirements
{
    public class RequiredClaimRequirement : IAuthorizationRequirement
    {
        public string ClaimType { get; }
        public string ClaimValue { get; }

        public RequiredClaimRequirement(string claimType, string claimValue)
        {
            ClaimType = claimType;
            ClaimValue = claimValue;
        }
    }
}
