using Microsoft.AspNetCore.Authorization;

namespace backend_api.Authorize.Requirements
{
    public class FirstNameAuthRequirement : IAuthorizationRequirement
    {

        public FirstNameAuthRequirement(string name)
        {
            Name = name;
        }
        public string Name { get; set; }
    }
}
