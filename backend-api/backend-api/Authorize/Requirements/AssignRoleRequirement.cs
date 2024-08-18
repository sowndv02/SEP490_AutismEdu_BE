using Microsoft.AspNetCore.Authorization;

namespace backend_api.Authorize.Requirements
{
    public class AssignRoleRequirement : IAuthorizationRequirement
    {
        public string ClaimType { get; }
        public string ClaimValue { get; }

        public AssignRoleRequirement(string claimType, string claimValue)
        {
            ClaimType = claimType;
            ClaimValue = claimValue;
        }
    }
}
