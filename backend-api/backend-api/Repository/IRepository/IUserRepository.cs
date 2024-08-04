using backend_api.Models;
using backend_api.Models.DTOs;
using Microsoft.AspNetCore.Identity;

namespace backend_api.Repository.IRepository
{
    public interface IUserRepository
    {
        bool IsUniqueUser(string username);
        Task<TokenDTO> Login(LoginRequestDTO loginRequestDTO, bool checkPassword = true);
        Task<ApplicationUser> Register(RegisterationRequestDTO registerationRequestDTO);
        Task<TokenDTO> RefreshAccessToken(TokenDTO tokenDTO);
        Task RevokeRefreshToken(TokenDTO tokenDTO);

        Task<ApplicationUser> GetAsync(string userId);
        Task<ApplicationUser> GetUserByEmailAsync(string email);
        Task<ApplicationUser> LockoutUser(string userId);
        Task<ApplicationUser> UnlockUser(string userId);
        Task<ApplicationUser> UpdatePasswordAsync(UpdatePasswordRequestDTO updatePasswordRequestDTO);
        Task<ApplicationUser> UpdateAsync(ApplicationUser user);
        Task<ApplicationUser> CreateAsync(ApplicationUser user, string password);
        Task<bool> RemoveAsync(string userId);
        Task<List<ApplicationUser>> GetAllAsync();
    }
}
