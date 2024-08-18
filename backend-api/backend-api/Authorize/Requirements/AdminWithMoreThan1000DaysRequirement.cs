using Microsoft.AspNetCore.Authorization;

namespace backend_api.Authorize.Requirements
{
    public class AdminWithMoreThan1000DaysRequirement : IAuthorizationRequirement
    {

        public AdminWithMoreThan1000DaysRequirement(int days)
        {
            Days = days;
        }
        public int Days { get; set; }
    }
}
