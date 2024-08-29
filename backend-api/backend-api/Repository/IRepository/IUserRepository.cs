using backend_api.Models;
using backend_api.Models.DTOs;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Security.Claims;

namespace backend_api.Repository.IRepository
{
    public interface IUserRepository
    {
        bool IsUniqueUser(string username);
        Task<TokenDTO> Login(LoginRequestDTO loginRequestDTO, bool checkPassword = true, string refreshTokenGoogle = null);
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
        Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user);
        Task<bool> ResetPasswordAsync(ApplicationUser user, string code, string password);
        Task<bool> ConfirmEmailAsync(ApplicationUser user, string code);
        Task<GoogleJsonWebSignature.Payload> VerifyGoogleToken(string token);
        Task<(int TotalCount, List<ApplicationUser> Users)> GetUsersForClaimAsync(int claimId, int takeValue = 4, int pageSize = 0, int pageNumber = 0);
        Task<(int TotalCount, List<ApplicationUser> Users)> GetUsersInRole(string roleName, int takeValue = 4, int pageSize = 0, int pageNumber = 0);
        Task<string> GetRefreshTokenGoogleValid(string userId);
    }
}
