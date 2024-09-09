using backend_api.Data;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Repository.IRepository;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;

namespace backend_api.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IClaimRepository _claimRepository;
        private string secretKey = string.Empty;

        public UserRepository(ApplicationDbContext context, IConfiguration configuration, UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager, SignInManager<ApplicationUser> signInManager, IClaimRepository claimRepository)
        {
            _claimRepository = claimRepository;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
            secretKey = configuration.GetValue<string>("ApiSettings:JWT:Secret");
        }

        public async Task RevokeRefreshTokenGoogleAsync(string userId)
        {
            try
            {
                var list = await _context.RefreshTokens.Where(x => x.UserId == userId && x.TokenType == SD.GOOGLE_REFRESH_TOKEN && x.IsValid).ToListAsync();
                foreach (var item in list)
                {
                    item.IsValid = false;
                }
                _context.RefreshTokens.UpdateRange(list);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task<string> SaveRefreshTokenGoogleAsync(RefreshToken refreshToken)
        {
            try
            {
                await _context.RefreshTokens.AddAsync(refreshToken);
                await _context.SaveChangesAsync();
                return refreshToken.Refresh_Token;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<(int TotalCount, List<ApplicationUser> Users)> GetUsersForClaimAsync(int claimId, int takeValue = 4, int pageSize = 0, int pageNumber = 0)
        {
            try
            {
                var claim = await _claimRepository.GetAsync(x => x.Id == claimId);
                var list = await _userManager.GetUsersForClaimAsync(new Claim(claim.ClaimType, claim.ClaimValue));
                if (pageSize == 0 && pageNumber == 0)
                {
                    return (list.Count, list.Take(takeValue).ToList());
                }
                else
                {
                    return (list.Count, list.Skip(pageSize * (pageNumber - 1)).Take(pageSize).ToList());
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<(int TotalCount, List<ApplicationUser> Users)> GetUsersInRole(string roleName, int takeValue = 4, int pageSize = 0, int pageNumber = 0)
        {
            try
            {
                var list = await _userManager.GetUsersInRoleAsync(roleName);
                if (pageSize == 0 && pageNumber == 0)
                {
                    return (list.Count, list.Take(takeValue).ToList());
                }
                else
                {
                    return (list.Count, list.Skip(pageSize * (pageNumber - 1)).Take(pageSize).ToList());
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> ConfirmEmailAsync(ApplicationUser user, string code)
        {
            try
            {
                var result = await _userManager.ConfirmEmailAsync(user, code);
                if (result.Succeeded)
                {
                    return true;
                }
                else
                {
                    throw new Exception(result.Errors.FirstOrDefault().Description);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user)
        {
            try
            {
                return await _userManager.GenerateEmailConfirmationTokenAsync(user);
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
                var result = await _userManager.ResetPasswordAsync(user, code, password);
                if (result.Succeeded)
                {
                    return true;
                }
                else
                {
                    throw new Exception(result.Errors.FirstOrDefault().Description);
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
                if (user == null)
                {
                    throw new ArgumentException("User not found");
                }

                List<ApplicationClaim> claims = _context.ApplicationClaims.Where(c => claimIds.Contains(c.Id)).ToList();

                // Get the user's current claims
                var currentClaims = await _userManager.GetClaimsAsync(user);

                // Determine which claims are missing
                var existingClaims = currentClaims
                    .Select(c => new { ClaimType = c.Type, ClaimValue = c.Value })
                    .ToHashSet();

                // Use a HashSet for efficient lookup
                var missingClaims = claims
                    .Where(c => !existingClaims.Contains(new { c.ClaimType, c.ClaimValue }))
                    .Select(c => new Claim(c.ClaimType, c.ClaimValue))
                    .ToList();


                if (missingClaims.Any())
                {
                    var result = await _userManager.AddClaimsAsync(user, missingClaims);
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

        public async Task<bool> RemoveClaimByUserId(string userId, List<int> userClaimIds)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    throw new ArgumentException("User not found");
                }
                var userClaims = _context.UserClaims.Where(c => userClaimIds.Contains(c.Id)).ToList();
                var result = await _userManager.RemoveClaimsAsync(user, userClaims.Select(x => new Claim(x.ClaimType, x.ClaimValue)));
                if (!result.Succeeded) return false;
                return true;
            }
            catch (Exception ex)
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

        public async Task<TokenDTO> Login(LoginRequestDTO loginRequestDTO, bool checkPassword = true, string refreshTokenGoogle = null)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName.ToLower() == loginRequestDTO.Email.ToLower());
            if (user == null)
            {
                return new TokenDTO()
                {
                    AccessToken = "",
                    RefreshToken = ""
                };
            }

            // Check if the user's email is confirmed
            if (!user.EmailConfirmed)
            {
                throw new MissingMemberException("Email is not confirmed.");
            }

            // Check if the user is locked out
            if (user.LockoutEnd != null && user.LockoutEnd.Value > DateTime.Now)
            {
                throw new Exception("User account is locked out.");
            }

            bool isValid = false;
            if (checkPassword)
                isValid = await _userManager.CheckPasswordAsync(user, loginRequestDTO.Password);
            else
                isValid = true;

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
            if (!string.IsNullOrEmpty(refreshTokenGoogle) && !checkPassword)
            {
                await RevokeRefreshTokenGoogleAsync(user.Id);
                await SaveRefreshTokenGoogleAsync(new RefreshToken()
                {
                    UserId = user.Id,
                    Refresh_Token = refreshTokenGoogle,
                    ExpiresAt = DateTime.Now.AddYears(1),
                    IsValid = true,
                    JwtTokenId = jwtTokenId,
                    TokenType = SD.GOOGLE_REFRESH_TOKEN
                });
            }
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
                UserName = registerationRequestDTO.Email,
                Email = registerationRequestDTO.Email,
                NormalizedEmail = registerationRequestDTO.Email,
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
                    if (objReturn.LockoutEnd == null || objReturn.LockoutEnd <= DateTime.Now)
                        objReturn.IsLockedOut = false;
                    else objReturn.IsLockedOut = true;
                    var user_claim = await _userManager.GetClaimsAsync(user) as List<Claim>;
                    var user_role = await _userManager.GetRolesAsync(user) as List<string>;
                    user.Role = String.Join(",", user_role);
                    user.UserClaim = String.Join(",", user_claim.Select(x => $"{x.Type}-{x.Value}").ToList());
                    return objReturn;
                }
                else
                {
                    throw new Exception(result.Errors.FirstOrDefault().Description);
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
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Role, roles.FirstOrDefault()),
                new Claim(JwtRegisteredClaimNames.Jti,  jwtTokenId)
            };
            authClaims.AddRange(userClaims);
            var token = new JwtSecurityToken(
                issuer: _configuration["ApiSettings:JWT:ValidIssuer"],
                audience: _configuration["ApiSettings:JWT:ValidAudience"],
                expires: DateTime.UtcNow.AddMinutes(30),
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
            if (existingRefreshToken.ExpiresAt < DateTime.Now)
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
                ExpiresAt = DateTime.Now.AddDays(100),
                Refresh_Token = Guid.NewGuid() + "-" + Guid.NewGuid(),
                TokenType = SD.APPLICATION_REFRESH_TOKEN
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
                    if (user.LockoutEnd == null || user.LockoutEnd <= DateTime.Now)
                        user.IsLockedOut = false;
                    else user.IsLockedOut = true;
                    var user_claim = await _userManager.GetClaimsAsync(user) as List<Claim>;
                    var user_role = await _userManager.GetRolesAsync(user) as List<string>;
                    user.Role = String.Join(",", user_role);
                    user.UserClaim = String.Join(",", user_claim.Select(x => $"{x.Type}-{x.Value}").ToList());
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
                    if (user.LockoutEnd == null || user.LockoutEnd <= DateTime.Now)
                        user.IsLockedOut = false;
                    else user.IsLockedOut = true;
                    var user_claim = await _userManager.GetClaimsAsync(user) as List<Claim>;
                    var user_role = await _userManager.GetRolesAsync(user) as List<string>;
                    user.Role = String.Join(",", user_role);
                    user.UserClaim = String.Join(",", user_claim.Select(x => $"{x.Type}-{x.Value}").ToList());
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
                    if (user.LockoutEnd == null || user.LockoutEnd <= DateTime.Now)
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
                    if (user.LockoutEnd == null || user.LockoutEnd <= DateTime.Now)
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
            var user = await _userManager.FindByEmailAsync(updatePasswordRequestDTO.Email);

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

            if (objReturn.LockoutEnd == null || objReturn.LockoutEnd <= DateTime.Now)
                objReturn.IsLockedOut = false;
            else objReturn.IsLockedOut = true;
            var user_role = await _userManager.GetRolesAsync(user) as List<string>;
            var user_claim = await _userManager.GetClaimsAsync(user) as List<Claim>;
            var claimString = user_claim.Select(x => $"{x.Type}-{x.Value}").ToList();
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
                UserName = user.Email,
                PhoneNumber = user.PhoneNumber,
                FullName = user.FullName,
                PasswordHash = password,
                Email = user.Email,
                NormalizedEmail = user.Email,
                LockoutEnabled = true,
                LockoutEnd = user.IsLockedOut ? DateTime.MaxValue : null,
                ImageUrl = user.ImageUrl,
                ImageLocalPathUrl = user.ImageLocalPathUrl,
                ImageLocalUrl = user.ImageLocalUrl,
                UserType = user.UserType,
                CreatedDate = DateTime.Now,
                EmailConfirmed = user.UserType == SD.GOOGLE_USER ? true : false,
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
                        if (!_roleManager.RoleExistsAsync(SD.USER_ROLE).GetAwaiter().GetResult())
                        {
                            await _roleManager.CreateAsync(new IdentityRole(SD.USER_ROLE));
                        }
                        await _userManager.AddToRoleAsync(obj, SD.USER_ROLE);
                    }
                }
                else
                {
                    if (!_roleManager.RoleExistsAsync(SD.USER_ROLE).GetAwaiter().GetResult())
                    {
                        await _roleManager.CreateAsync(new IdentityRole(SD.USER_ROLE));
                    }
                    await _userManager.AddToRoleAsync(obj, SD.USER_ROLE);
                }
                var objReturn = _context.ApplicationUsers.FirstOrDefault(u => u.UserName == user.Email);

                if (objReturn.LockoutEnd == null || objReturn.LockoutEnd <= DateTime.Now)
                    objReturn.IsLockedOut = false;
                else objReturn.IsLockedOut = true;

                var user_role = await _userManager.GetRolesAsync(user) as List<string>;
                var user_claim = await _userManager.GetClaimsAsync(user) as List<Claim>;
                var claimString = user_claim.Select(x => $"{x.Type}-{x.Value}").ToList();
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
                if (user.LockoutEnd == null || user.LockoutEnd <= DateTime.Now)
                    user.IsLockedOut = false;
                else user.IsLockedOut = true;
                var user_role = await _userManager.GetRolesAsync(user) as List<string>;
                var user_claim = await _userManager.GetClaimsAsync(user) as List<Claim>;
                var claimString = user_claim.Select(x => $"{x.Type}-{x.Value}").ToList();
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
                    var userClaims = await _context.UserClaims.Where(x => x.UserId == userId).
                        Select(c => new UserClaim
                        {
                            Id = c.Id,
                            UserId = c.UserId,
                            ClaimType = c.ClaimType,
                            ClaimValue = c.ClaimValue
                        }).ToListAsync();
                    return userClaims;
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

        public async Task<GoogleJsonWebSignature.Payload> VerifyGoogleToken(string token)
        {
            try
            {
                return await GoogleJsonWebSignature.ValidateAsync(token);
            }
            catch (Exception)
            {
                return null; //Invalid token or error
            }
        }
        public async Task<string> GetRefreshTokenGoogleValid(string userId)
        {
            try
            {
                var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.IsValid && x.TokenType == SD.GOOGLE_REFRESH_TOKEN && x.UserId == userId);

                if (refreshToken == null) return null;

                return refreshToken.Refresh_Token;
            }
            catch (Exception)
            {
                return null; //Invalid token or error
            }
        }

        public async Task<bool> RemoveRoleByUserId(string userId, List<string> userRoleIds)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    throw new ArgumentException("User not found");
                }
                var roles = new List<IdentityRole>();
                var userRoles = _context.UserRoles.Where(c => userRoleIds.Contains(c.RoleId)).ToList();
                foreach (var roleId in userRoleIds)
                {
                    roles.Add(await _roleManager.FindByIdAsync(roleId));
                }
                var result = await _userManager.RemoveFromRolesAsync(user, roles.Select(x => x.Name));
                if (!result.Succeeded) return false;
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
                if (user == null)
                {
                    throw new ArgumentException("User not found");
                }

                List<IdentityRole> roles = _roleManager.Roles.Where(c => roleIds.Contains(c.Id)).ToList();

                var currentRoles = await _userManager.GetRolesAsync(user);

                var missingRoles = roles
                    .Where(c => !currentRoles.Contains(c.Name))
                    .Select(c => c.Name)
                    .ToList();


                if (missingRoles.Any())
                {
                    var result = await _userManager.AddToRolesAsync(user, missingRoles);
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


        public async Task<List<IdentityRole>> GetRoleByUserId(string userId)
        {
            try
            {
                if (!string.IsNullOrEmpty(userId))
                {
                    List<IdentityRole> roles = new List<IdentityRole>();
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user == null) throw new Exception("user not found");
                    var roleNames = await _userManager.GetRolesAsync(user);
                    foreach (var name in roleNames)
                    {
                        var r = await _roleManager.FindByNameAsync(name);
                        roles.Add(r);
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
    }
}
