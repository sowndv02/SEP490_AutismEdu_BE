using backend_api.Models.DTOs;
using backend_api.Models;
using Microsoft.AspNetCore.Identity;
using System.Linq.Expressions;

namespace backend_api.Repository.IRepository
{
    public interface IClaimRepository : IRepository<ApplicationClaim>
    {
        Task<ApplicationClaim> UpdateAsync(ApplicationClaim claim);
        int GetTotalClaim();

    }
}
