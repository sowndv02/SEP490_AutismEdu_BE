using Microsoft.AspNetCore.Authorization;

namespace backend_api.Authorize.Requirements
{
    public class AssignRoleOrClaimRequirement : IAuthorizationRequirement
    {
        public string ClaimType { get; }
        public string ClaimValue { get; }

        public AssignRoleOrClaimRequirement(string claimType, string claimValue)
        {
            ClaimType = claimType;
            ClaimValue = claimValue;
        }
    }
}
