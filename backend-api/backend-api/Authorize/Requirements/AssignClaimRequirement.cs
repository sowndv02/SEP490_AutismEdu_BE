using Microsoft.AspNetCore.Authorization;

namespace backend_api.Authorize.Requirements
{
    public class AssignClaimRequirement : IAuthorizationRequirement
    {
        public string ClaimType { get; }
        public string ClaimValue { get; }

        public AssignClaimRequirement(string claimType, string claimValue)
        {
            ClaimType = claimType;
            ClaimValue = claimValue;
        }
    }
}
