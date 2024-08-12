using backend_api.Models.DTOs;
using backend_api.Models;
using Microsoft.AspNetCore.Identity;
using System.Linq.Expressions;

namespace backend_api.Repository.IRepository
{
    public interface IClaimRepository
    {
        Task<ApplicationClaim> GetAsync(int claimId);
        Task<ApplicationClaim> CreateAsync(ApplicationClaim claim);
        Task<ApplicationClaim> UpdateAsync(ApplicationClaim claim);
        Task<ApplicationClaim> GetByIdAsync(int id);
        Task<bool> RemoveAsync(ApplicationClaim claim);
        Task<List<ApplicationClaim>> GetAllAsync();
    }
}
