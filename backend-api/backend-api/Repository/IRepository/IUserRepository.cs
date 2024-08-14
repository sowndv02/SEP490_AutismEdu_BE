using backend_api.Models;
using backend_api.Models.DTOs;
using Microsoft.AspNetCore.Identity;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Security.Claims;

namespace backend_api.Repository.IRepository
{
    public interface IUserRepository
    {
        bool IsUniqueUser(string username);
        Task<TokenDTO> Login(LoginRequestDTO loginRequestDTO, bool checkPassword = true);
        Task<ApplicationUser> Register(RegisterationRequestDTO registerationRequestDTO);
        Task<TokenDTO> RefreshAccessToken(TokenDTO tokenDTO);
        Task RevokeRefreshToken(TokenDTO tokenDTO);

        Task<ApplicationUser> GetAsync(Expression<Func<ApplicationUser, bool>>? filter = null, bool tracked = true, string? includeProperties = null);
        Task<List<UserClaim>> GetClaimByUserIdAsync(string userId);
        Task<ApplicationUser> GetUserByEmailAsync(string email);
        Task<ApplicationUser> LockoutUser(string userId);
        Task<ApplicationUser> UnlockUser(string userId);
        Task<ApplicationUser> UpdatePasswordAsync(UpdatePasswordRequestDTO updatePasswordRequestDTO);
        Task<ApplicationUser> UpdateAsync(ApplicationUser user);
        Task<ApplicationUser> CreateAsync(ApplicationUser user, string password);
        Task<bool> RemoveAsync(string userId);
        int GetTotalUser();
        Task<List<ApplicationUser>> GetAllAsync(Expression<Func<ApplicationUser, bool>>? filter = null, string? includeProperties = null, int pageSize = 10, int pageNumber = 1);
        Task<List<ApplicationUser>> GetListUserByClaim(ApplicationClaim claim);
        Task<bool> RemoveClaimByUserId(string userId, List<int> userClaimIds);
        Task<bool> AddClaimToUser(string userId, List<int> claimIds);
        Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user);
        Task<bool> ResetPasswordAsync(ApplicationUser user, string code, string password);

    }
}
