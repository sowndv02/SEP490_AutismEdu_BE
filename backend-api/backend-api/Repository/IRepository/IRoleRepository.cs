using Microsoft.AspNetCore.Identity;
using System.Linq.Expressions;

namespace backend_api.Repository.IRepository
{
    public interface IRoleRepository
    {
        Task CreateAsync(IdentityRole role);
        Task<List<IdentityRole>> GetAllAsync();
        Task<IdentityRole> GetByNameAsync(string name);
        Task<IdentityRole> GetByIdAsync(string roleId);
        Task<IdentityRole> GetRoleByUserId(string userId);
        Task<bool> RemoveAsync(IdentityRole role);
    }
}
