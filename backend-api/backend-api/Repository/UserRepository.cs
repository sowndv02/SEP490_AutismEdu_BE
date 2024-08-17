using AutoMapper;
using backend_api.Data;
using backend_api.Models.DTOs;
using backend_api.Models;
using backend_api.Repository.IRepository;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Linq.Expressions;
using System.Diagnostics;

namespace backend_api.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private string secretKey = string.Empty;

        public UserRepository(ApplicationDbContext context, IConfiguration configuration, UserManager<ApplicationUser> userManager, IMapper mapper, RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
            _mapper = mapper;
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
            secretKey = configuration.GetValue<string>("ApiSettings:JWT:Secret");
        }


        public async Task<List<IdentityRole>> GetRoleByUserIdAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);

                if (user != null)
                {
                    var roleNames = await _userManager.GetRolesAsync(user);
                    var roles = new List<IdentityRole>();

                    foreach (var roleName in roleNames)
                    {
                        var role = await _roleManager.FindByNameAsync(roleName);
                        if (role != null)
                        {
                            roles.Add(role);
                        }
                    }

                    return roles;
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> RemoveRoleByUserId(string userId, List<string> userRoleIds)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                foreach (var roleId in userRoleIds)
                {
                    var role = await _roleManager.FindByIdAsync(roleId);
                    if (role == null)
                    {
                        continue;
                    }

                    var result = await _userManager.RemoveFromRoleAsync(user, role.Name);
                    if (!result.Succeeded)
                    {
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> AddRoleToUser(string userId, List<string> roleIds)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                foreach (var roleId in roleIds)
                {
                    var role = await _roleManager.FindByIdAsync(roleId);
                    if (role == null)
                    {
                        continue;
                    }

                    var result = await _userManager.AddToRoleAsync(user, role.Name);
                    if (!result.Succeeded)
                    {
                        return false;
                    }
                }
                return true;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user)
        {
            try
            {
                return await _userManager.GeneratePasswordResetTokenAsync(user);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> ResetPasswordAsync(ApplicationUser user, string code, string password)
        {
            try
            {
                var result =  await _userManager.ResetPasswordAsync(user, code, password);
                if (result.Succeeded)
                {
                    return true;
                }
                else
                {
                    throw new Exception(result.Errors.FirstOrDefault().ToString());
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> AddClaimToUser(string userId, List<int> claimIds)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                List<ApplicationClaim> claims = _context.ApplicationClaims.Where(c => claimIds.Contains(c.Id)).ToList();
                var result = await _userManager.AddClaimsAsync(user, claims.Select(y => new Claim(y.ClaimType, y.ClaimValue)));
                if (!result.Succeeded) return false;
                return true;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> RemoveClaimByUserId(string userId, List<int> userClaimIds)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                var userClaims = _context.UserClaims.Where(c => userClaimIds.Contains(c.Id)).ToList();
                var result = await _userManager.RemoveClaimsAsync(user, userClaims.Select(x => new Claim(x.ClaimType, x.ClaimValue)));
                if (!result.Succeeded) return false;
                return true;
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<ApplicationUser>> GetListUserByClaim(ApplicationClaim claim)
        {
            try
            {
                var users = await _userManager.GetUsersForClaimAsync(new Claim(claim.ClaimType, claim.ClaimValue));
                return users.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public bool IsUniqueUser(string username)
        {
            var user = _context.Users.FirstOrDefault(x => x.UserName == username);
            if (user == null)
            {
                return true;
            }
            return false;
        }

        public async Task<TokenDTO> Login(LoginRequestDTO loginRequestDTO, bool checkPassword = true)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName.ToLower() == loginRequestDTO.UserName.ToLower());
            if (user == null)
            {
                return new TokenDTO()
                {
                    AccessToken = "",
                    RefreshToken = ""
                };
            }
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                return null;
            }
            bool isValid = false;
            if (checkPassword)
                isValid = await _userManager.CheckPasswordAsync(user, loginRequestDTO.Password);
            else isValid = true;
            if (!isValid)
            {
                return new TokenDTO()
                {
                    AccessToken = ""
                };
            }
            var jwtTokenId = $"JTI{Guid.NewGuid()}";
            var accessToken = await GetAccessToken(user, jwtTokenId);
            var refreshToken = await CreateNewRefreshToken(user.Id, jwtTokenId);

            TokenDTO tokenDTO = new()
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
            return tokenDTO;
        }


        public async Task<ApplicationUser> Register(RegisterationRequestDTO registerationRequestDTO)
        {
            ApplicationUser user = new()
            {
                UserName = registerationRequestDTO.UserName,
                Email = registerationRequestDTO.UserName,
                NormalizedEmail = registerationRequestDTO.UserName,
                PasswordHash = registerationRequestDTO.Password,
                LockoutEnabled = true
            };
            try
            {
                var result = await _userManager.CreateAsync(user, registerationRequestDTO.Password);
                if (result.Succeeded)
                {
                    if (!_roleManager.RoleExistsAsync(registerationRequestDTO.Role).GetAwaiter().GetResult())
                    {
                        await _roleManager.CreateAsync(new IdentityRole(registerationRequestDTO.Role));
                    }

                    await _userManager.AddToRoleAsync(user, registerationRequestDTO.Role);
                    ApplicationUser objReturn = await _context.ApplicationUsers.FirstOrDefaultAsync(u => u.Email == user.Email);
                    if (objReturn.LockoutEnd <= DateTime.Now)
                        objReturn.IsLockedOut = false;
                    else objReturn.IsLockedOut = true;
                    var user_claim = await _userManager.GetClaimsAsync(user) as List<Claim>;
                    var user_role = await _userManager.GetRolesAsync(user) as List<string>;
                    user.Role = String.Join(",", user_role);
                    user.UserClaim = String.Join(",", user_claim.Select(x => x.Type));
                    return objReturn;
                }
                else
                {
                    throw new Exception(result.Errors.FirstOrDefault().ToString());
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private async Task<string> GetAccessToken(ApplicationUser user, string jwtTokenId)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var authenKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var userClaims = await _userManager.GetClaimsAsync(user);
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, roles.FirstOrDefault()),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, jwtTokenId)
            };
            authClaims.AddRange(userClaims);
            var token = new JwtSecurityToken(
                issuer: _configuration["ApiSettings:JWT:ValidIssuer"],
                audience: _configuration["ApiSettings:JWT:ValidAudience"],
                expires: DateTime.Now.AddMinutes(20),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authenKey, SecurityAlgorithms.HmacSha512Signature)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<TokenDTO> RefreshAccessToken(TokenDTO tokenDTO)
        {
            // Find an existing refresh token

            var existingRefreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Refresh_Token == tokenDTO.RefreshToken);
            if (existingRefreshToken == null)
            {
                return new TokenDTO();
            }

            // Compare data from existing refresh token provided and if there is any missmatch then consider it as a fraud
            var isTokenValid = GetAccessTokenData(tokenDTO.AccessToken, existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);
            if (!isTokenValid)
            {
                await MarkTokenAsInValid(existingRefreshToken);
                return new TokenDTO();
            }

            // When someone tries to use not valid refresh token, fraud possible
            if (!existingRefreshToken.IsValid)
            {
                MarkAllTokenInChainAsInValid(existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);
                return new TokenDTO();
            }


            // If jus expired then mark as invalid and return empty
            if (existingRefreshToken.ExpiresAt < DateTime.UtcNow)
            {
                await MarkTokenAsInValid(existingRefreshToken);
                return new TokenDTO();
            }

            // Replace old refresh with a new one with updated expire date
            var newRefreshToken = await CreateNewRefreshToken(existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);


            // revoke existing refresh token
            await MarkTokenAsInValid(existingRefreshToken);

            // generate new access token
            var applicationUser = _context.ApplicationUsers.FirstOrDefault(x => x.Id == existingRefreshToken.UserId);
            if (applicationUser == null)
                return new TokenDTO();

            var newAccessToken = await GetAccessToken(applicationUser, existingRefreshToken.JwtTokenId);
            return new TokenDTO()
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }

        private async Task<string> CreateNewRefreshToken(string userId, string tokenId)
        {
            RefreshToken refreshToken = new()
            {
                IsValid = true,
                UserId = userId,
                JwtTokenId = tokenId,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                Refresh_Token = Guid.NewGuid() + "-" + Guid.NewGuid()
            };

            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();
            return refreshToken.Refresh_Token;
        }

        private bool GetAccessTokenData(string accessToken, string expectedUserId, string expectedTokenId)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwt = tokenHandler.ReadJwtToken(accessToken);

                var jwtTolenId = jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Jti).Value;
                var userId = jwt.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub).Value;
                return userId == expectedUserId && jwtTolenId == expectedTokenId;

            }
            catch
            {
                return false;
            }
        }


        private void MarkAllTokenInChainAsInValid(string userId, string tokenId)
        {
            var chainRecords = _context.RefreshTokens.Where(x => x.UserId == userId
                && x.JwtTokenId == tokenId);
            foreach (var item in chainRecords)
            {
                item.IsValid = false;
            }
            _context.UpdateRange(chainRecords);
            _context.SaveChanges();
        }

        private async Task MarkTokenAsInValid(RefreshToken refreshToken)
        {
            refreshToken.IsValid = false;
            await _context.SaveChangesAsync();
        }

        public async Task RevokeRefreshToken(TokenDTO tokenDTO)
        {
            var existingRefreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(_ => _.Refresh_Token == tokenDTO.RefreshToken);
            if (existingRefreshToken == null)
                return;

            // Compare data from existing refresh and access token provided and 
            // if there is any missmatch then we should do nothing with refresh token

            var isTokenValid = GetAccessTokenData(tokenDTO.AccessToken, existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);
            if (!isTokenValid)
            {
                return;
            }
            MarkAllTokenInChainAsInValid(existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);
        }

        public async Task<ApplicationUser> GetUserByEmailAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    if (user.LockoutEnd <= DateTime.Now)
                        user.IsLockedOut = false;
                    else user.IsLockedOut = true;
                    var user_claim = await _userManager.GetClaimsAsync(user) as List<Claim>;
                    var user_role = await _userManager.GetRolesAsync(user) as List<string>;
                    user.Role = String.Join(",", user_role);
                    user.UserClaim = String.Join(",", user_claim.Select(x => x.Type));
                    return user;
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<ApplicationUser> GetAsync(Expression<Func<ApplicationUser, bool>> filter = null, bool tracked = true, string? includeProperties = null)
        {
            try
            {
                IQueryable<ApplicationUser> query = _context.ApplicationUsers;
                if (!tracked)
                    query = query.AsNoTracking();

                if (filter != null)
                    query = query.Where(filter);

                if (includeProperties != null)
                {
                    foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        query = query.Include(includeProperty);
                    }
                }
                var user = await query.FirstOrDefaultAsync();
                if (user != null)
                {
                    if (user.LockoutEnd <= DateTime.Now)
                        user.IsLockedOut = false;
                    else user.IsLockedOut = true;
                    var user_claim = await _userManager.GetClaimsAsync(user) as List<Claim>;
                    var user_role = await _userManager.GetRolesAsync(user) as List<string>;
                    user.Role = String.Join(",", user_role);
                    user.UserClaim = String.Join(",", user_claim.Select(x => x.Type));
                    return user;
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<ApplicationUser> LockoutUser(string userId)
        {
            try
            {
                ApplicationUser user = _context.ApplicationUsers.FirstOrDefault(u => u.Id == userId);
                if (user != null)
                {
                    user.LockoutEnd = DateTime.Now.AddYears(1000);
                    await _context.SaveChangesAsync();
                    if (user.LockoutEnd <= DateTime.Now)
                        user.IsLockedOut = false;
                    else user.IsLockedOut = true;
                }
                
                return user;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<ApplicationUser> UnlockUser(string userId)
        {
            try
            {
                ApplicationUser user = _context.ApplicationUsers.FirstOrDefault(u => u.Id == userId);
                if (user != null)
                {
                    user.LockoutEnd = DateTime.Now;
                    await _context.SaveChangesAsync();
                    if (user.LockoutEnd <= DateTime.Now)
                        user.IsLockedOut = false;
                    else user.IsLockedOut = true;
                }
                
                return user;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<ApplicationUser> UpdatePasswordAsync(UpdatePasswordRequestDTO updatePasswordRequestDTO)
        {
            var user = await _userManager.FindByEmailAsync(updatePasswordRequestDTO.UserName);

            if (user == null)
            {
                throw new Exception("User not found.");
            }

            bool isValid = await _userManager.CheckPasswordAsync(user, updatePasswordRequestDTO.Password);

            if (!isValid)
            {
                throw new Exception("Invalid current password.");
            }

            var result = await _userManager.ChangePasswordAsync(user, updatePasswordRequestDTO.Password, updatePasswordRequestDTO.NewPassword);

            if (!result.Succeeded)
            {
                throw new Exception("Password change failed.");
            }
            var objReturn = _context.ApplicationUsers.FirstOrDefault(u => u.UserName == user.Email);

            if (objReturn.LockoutEnd <= DateTime.Now)
                objReturn.IsLockedOut = false;
            else objReturn.IsLockedOut = true;
            var user_role = await _userManager.GetRolesAsync(user) as List<string>;
            var user_claim = await _userManager.GetClaimsAsync(user) as List<Claim>;
            var claimString = user_claim.Select(x => x.Type);
            user.UserClaim = claimString.Count() != 0 ? String.Join(",", claimString) : "";
            user.Role = String.Join(",", user_role);
            return objReturn;
        }

        public async Task<ApplicationUser> UpdateAsync(ApplicationUser user)
        {
            try
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                if (user.LockoutEnd <= DateTime.Now)
                    user.IsLockedOut = false;
                else user.IsLockedOut = true;
                return user;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<ApplicationUser> CreateAsync(ApplicationUser user, string password)
        {
            ApplicationUser obj = new ApplicationUser
            {
                UserName = user.UserName,
                PhoneNumber = user.PhoneNumber,
                FullName = user.FullName,
                PasswordHash = password,
                Email = user.Email,
                NormalizedEmail = user.Email,
                LockoutEnabled = true,
                LockoutEnd = user.IsLockedOut ? DateTime.MaxValue : null
            };
            var result = await _userManager.CreateAsync(obj, password);

            if (result.Succeeded)
            {
                var roleId = user.RoleId;
                if (roleId != null)
                {
                    user.Role = _roleManager.FindByIdAsync(roleId).GetAwaiter().GetResult().Name;
                    if (user.Role != null)
                    {
                        await _userManager.AddToRoleAsync(obj, user.Role);
                    }
                    else
                    {
                        if (!_roleManager.RoleExistsAsync(SD.User).GetAwaiter().GetResult())
                        {
                            await _roleManager.CreateAsync(new IdentityRole(SD.User));
                        }
                        await _userManager.AddToRoleAsync(obj, SD.User);
                    }
                }
                else
                {
                    if (!_roleManager.RoleExistsAsync(SD.User).GetAwaiter().GetResult())
                    {
                        await _roleManager.CreateAsync(new IdentityRole(SD.User));
                    }
                    await _userManager.AddToRoleAsync(obj, SD.User);
                }
                var objReturn = _context.ApplicationUsers.FirstOrDefault(u => u.UserName == user.Email);

                if (objReturn.LockoutEnd <= DateTime.Now)
                    objReturn.IsLockedOut = false;
                else objReturn.IsLockedOut = true;

                var user_role = await _userManager.GetRolesAsync(user) as List<string>;
                var user_claim = await _userManager.GetClaimsAsync(user) as List<Claim>;
                var claimString = user_claim.Select(x => x.Type);
                user.UserClaim = claimString.Count() != 0 ? String.Join(",", claimString) : "";
                user.Role = String.Join(",", user_role);
                return objReturn;
            }
            else
            {
                var mesg = result.Errors;
            }
            return null;
        }

        public async Task<bool> RemoveAsync(string userId)
        {
            try
            {
                var user = _context.ApplicationUsers.FirstOrDefault(u => u.Id == userId);
                if (user == null)
                    return false;

                _context.ApplicationUsers.Remove(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<ApplicationUser>> GetAllAsync(Expression<Func<ApplicationUser, bool>>? filter = null, string? includeProperties = null,
            int pageSize = 10, int pageNumber = 1)
        {

            IQueryable<ApplicationUser> query = _context.ApplicationUsers;

            if (filter != null)
                query = query.Where(filter);

            if (includeProperties != null)
            {
                foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty);
                }
            }
            query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
            var users = await query.ToListAsync();
            foreach (var user in users)
            {
                if (user.LockoutEnd <= DateTime.Now)
                    user.IsLockedOut = false;
                else user.IsLockedOut = true;
                var user_role = await _userManager.GetRolesAsync(user) as List<string>;
                var user_claim = await _userManager.GetClaimsAsync(user) as List<Claim>;
                var claimString = user_claim.Select(x => x.Type);
                user.UserClaim = claimString.Count() != 0 ? String.Join(",", claimString) : "";
                user.Role = String.Join(",", user_role);
            }
            return users;
        }

        public async Task<List<UserClaim>> GetClaimByUserIdAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                
                if (user != null)
                {
                    var userClaims = await _context.UserClaims.Where(x => x.UserId == userId).ToListAsync();
                    return _mapper.Map<List<UserClaim>>(userClaims);
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public int GetTotalUser()
        {
            try
            {
                return _context.ApplicationUsers.ToList().Count();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        
    }
}
