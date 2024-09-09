using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Repository.IRepository;
using backend_api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace backend_api.Controllers
{
    [Route("api/v{version:apiVersion}/Auth")]
    [ApiController]
    [ApiVersionNeutral]
    public class AuthController : ControllerBase
    {
        private readonly DateTimeEncryption _dateTimeEncryption;
        private readonly TokenEcryption _tokenEncryption;
        private readonly IUserRepository _userRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        private string audience = string.Empty;
        private readonly IEmailSender _emailSender;
        private static int ValidateTime = 0;
        private static string clientId = string.Empty;
        private static string clientSecret = string.Empty;
        public AuthController(IUserRepository userRepository, IMapper mapper,
            IConfiguration configuration, IEmailSender emailSender, DateTimeEncryption dateTimeEncryption,
            TokenEcryption tokenEncryption)
        {
            ValidateTime = configuration.GetValue<int>("APIConfig:ValidateTime");
            clientId = configuration.GetValue<string>("Authentication:Google:ClientId");
            clientSecret = configuration.GetValue<string>("Authentication:Google:ClientSecret");
            _dateTimeEncryption = dateTimeEncryption;
            _mapper = mapper;
            _userRepository = userRepository;
            _response = new();
            _emailSender = emailSender;
            _tokenEncryption = tokenEncryption;
        }

        [HttpPost("resend-confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendConfirmEmail(ResendConfirmEmailDTO model)
        {

            try
            {
                if (model == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { "Data invalid." };
                    return BadRequest(_response);
                }
                var user = await _userRepository.GetUserByEmailAsync(model.Email);
                if (user == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { $"User not found with email is {model.Email} invalid." };
                    return BadRequest(_response);
                }
                string code = await _userRepository.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = $"{SD.URL_FE}/confirm-register?userId={user.Id}&code={code}&security={_dateTimeEncryption.EncryptDateTime(DateTime.Now)}";
                await _emailSender.SendEmailAsync(user.Email, "Confirm Email", $"Expiration time 5 minutes. \nPlease confirm email by clicking here: <a href='{callbackUrl}'>link</a>");
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPost("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(ConfirmEmailDTO model)
        {

            try
            {
                if (model == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { "Data invalid." };
                    return BadRequest(_response);
                }
                var user = await _userRepository.GetAsync(x => x.Id == model.UserId);
                if (user == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { $"User not found with email is {model.UserId} invalid." };
                    return BadRequest(_response);
                }
                DateTime security = _dateTimeEncryption.DecryptDateTime(model.Security);
                if (DateTime.Now > security.AddMinutes(ValidateTime))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { "Link Expired." };
                    return BadRequest(_response);
                }
                var result = await _userRepository.ConfirmEmailAsync(user, model.Code);
                if (!result)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.InternalServerError;
                    _response.ErrorMessages = new List<string>() { "Internal server error!" };
                    return StatusCode((int)HttpStatusCode.InternalServerError, _response);
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordDTO model)
        {

            try
            {
                if (model == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { "Data invalid." };
                    return BadRequest(_response);
                }
                var user = await _userRepository.GetAsync(x => x.Id == model.UserId);
                if (user == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { $"User not found with UserId is {model.UserId} invalid." };
                    return BadRequest(_response);
                }
                else if (user.UserType == SD.GOOGLE_USER)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { $"User gooogle cannot forgot password." };
                    return NotFound(_response);
                }
                DateTime security = _dateTimeEncryption.DecryptDateTime(model.Security);
                if (DateTime.Now > security.AddMinutes(ValidateTime))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { "Link Expired." };
                    return BadRequest(_response);
                }
                var result = await _userRepository.ResetPasswordAsync(user, model.Code, model.Password);
                if (!result)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.InternalServerError;
                    _response.ErrorMessages = new List<string>() { "Internal server error!" };
                    return StatusCode((int)HttpStatusCode.InternalServerError, _response);
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDTO forgotPasswordDTO)
        {

            try
            {
                if (forgotPasswordDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { "Data invalid." };
                    return BadRequest(_response);
                }
                var user = await _userRepository.GetUserByEmailAsync(forgotPasswordDTO.Email);
                if (user == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { $"User not found with email is {forgotPasswordDTO.Email} invalid." };
                    return NotFound(_response);
                }
                else if (user.UserType == SD.GOOGLE_USER)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { $"User gooogle cannot forgot password." };
                    return NotFound(_response);
                }
                var code = await _userRepository.GeneratePasswordResetTokenAsync(user);

                var callbackUrl = $"{SD.URL_FE}/reset-password?userId={user.Id}&code={code}&security={_dateTimeEncryption.EncryptDateTime(DateTime.Now)}";

                await _emailSender.SendEmailAsync(forgotPasswordDTO.Email, "Reset password", $"Expiration time 5 minutes. \nPlease reset your password by clicking here: <a href='{callbackUrl}'>link</a>");

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDTO model)
        {
            try
            {
                var tokenDto = await _userRepository.Login(model);
                if (tokenDto == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { "User is currently locked out." };
                    return BadRequest(_response);
                }
                if (string.IsNullOrEmpty(tokenDto.AccessToken))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { "Username or password is incorrect" };
                    return BadRequest(_response);
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = tokenDto;
                return Ok(_response);
            }
            catch (MissingMemberException e)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.NotAcceptable;
                _response.ErrorMessages = new List<string>() { e.Message };
                return BadRequest(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterationRequestDTO model)
        {
            try
            {
                bool ifUserNameUnique = _userRepository.IsUniqueUser(model.Email);
                if (!ifUserNameUnique)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { "Username already exists" };
                    return BadRequest(_response);
                }

                var user = await _userRepository.Register(model);
                if (user == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { "Error while registering" };
                    return BadRequest(_response);
                }
                var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
                user.ImageLocalUrl = baseUrl + $"/{SD.URL_IMAGE_USER}/" + SD.IMAGE_DEFAULT_AVATAR_NAME;
                user.ImageUrl = SD.URL_IMAGE_DEFAULT_BLOB;
                user.ImageLocalPathUrl = @"wwwroot\UserImages\" + SD.IMAGE_DEFAULT_AVATAR_NAME;
                user.CreatedDate = DateTime.Now;
                user.UserType = SD.APPLICATION_USER;
                await _userRepository.UpdateAsync(user);

                string code = await _userRepository.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = $"{SD.URL_FE}/confirm-register?userId={user.Id}&code={code}&security={_dateTimeEncryption.EncryptDateTime(DateTime.Now)}";
                await _emailSender.SendEmailAsync(user.Email, "Confirm Email", $"Expiration time 5 minutes. \nPlease confirm email by clicking here: <a href='{callbackUrl}'>link</a>");

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> GetNewTokenFromRefreshToken([FromBody] TokenDTO model, [FromRoute] bool isRequiredGoogle = false)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var tokenDTOResponse = await _userRepository.RefreshAccessToken(model);
                    if (tokenDTOResponse == null || string.IsNullOrEmpty(tokenDTOResponse.AccessToken))
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        _response.ErrorMessages = new List<string>() { "Token Invalid" };
                        return BadRequest(_response);
                    }
                    if (isRequiredGoogle && model.AccessTokenGoogle != null)
                    {
                        var payload = await _userRepository.VerifyGoogleToken(_tokenEncryption.DecryptToken(model.AccessTokenGoogle));

                        if (payload == null)
                        {
                            _response.StatusCode = HttpStatusCode.BadRequest;
                            _response.IsSuccess = false;
                            _response.ErrorMessages = new List<string>() { "Invalid Google token." };
                            return BadRequest(_response);
                        }
                        var user = await _userRepository.GetUserByEmailAsync(payload.Email);
                        if (user == null)
                        {
                            _response.StatusCode = HttpStatusCode.BadRequest;
                            _response.IsSuccess = false;
                            _response.ErrorMessages = new List<string>() { "Invalid Google User." };
                            return BadRequest(_response);
                        }
                        var refreshToken = await _userRepository.GetRefreshTokenGoogleValid(user.Id);
                        if (refreshToken == null)
                        {
                            _response.StatusCode = HttpStatusCode.BadRequest;
                            _response.IsSuccess = false;
                            _response.ErrorMessages = new List<string>() { "User dont have any refresh token google valid." };
                            return BadRequest(_response);
                        }
                        var newAccessToken = await GetNewAccessTokenUsingRefreshTokenGoogle(refreshToken);
                        tokenDTOResponse.AccessTokenGoogle = _tokenEncryption.EncryptToken(newAccessToken);
                    }
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Result = tokenDTOResponse;
                    return Ok(_response);
                }
                else
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages = new List<string>() { "Error while refresh token" };
                    return BadRequest(_response);
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost("revoke")]
        public async Task<IActionResult> RevokeRefreshToken([FromBody] TokenDTO model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await _userRepository.RevokeRefreshToken(model);
                    _response.IsSuccess = true;
                    _response.StatusCode = HttpStatusCode.OK;
                    return Ok(_response);
                }
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages = new List<string> { "Invalid Input" };
                return BadRequest(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost("external-login")]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLogin([FromBody] ExternalLoginRequestDTO model)
        {
            try
            {
                var payload = await _userRepository.VerifyGoogleToken(model.Token);

                if (payload == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { "Invalid Google token." };
                    return BadRequest(_response);
                }
                if (payload.ExpirationTimeSeconds == 0)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages = new List<string>() { "Token google expired." };
                    return BadRequest(_response);
                }
                var user = await _userRepository.GetUserByEmailAsync(payload.Email);
                var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
                if (user == null)
                {
                    user = await _userRepository.CreateAsync(new ApplicationUser
                    {
                        Email = payload.Email,
                        Role = SD.USER_ROLE,
                        UserType = SD.GOOGLE_USER,
                        ImageUrl = payload.Picture,
                        ImageLocalPathUrl = @"wwwroot\UserImages\" + SD.IMAGE_DEFAULT_AVATAR_NAME,
                        ImageLocalUrl = baseUrl + $"/{SD.URL_IMAGE_USER}/" + SD.IMAGE_DEFAULT_AVATAR_NAME,
                        FullName = payload.Name,
                        EmailConfirmed = true,
                        IsLockedOut = false
                    }, PasswordGenerator.GeneratePassword());

                    if (user == null)
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        _response.ErrorMessages = new List<string>() { "Error while registering" };
                        return BadRequest(_response);
                    }
                    await _userRepository.UpdateAsync(user);
                }

                var tokenDto = await _userRepository.Login(new LoginRequestDTO()
                {
                    Email = user.UserName,
                    Password = user.PasswordHash
                }, false);


                if (tokenDto == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { "User is currently locked out." };
                    return BadRequest(_response);
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = tokenDto;
                return Ok(_response);
            }
            catch (MissingMemberException e)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.NotAcceptable;
                _response.ErrorMessages = new List<string>() { e.Message };
                return BadRequest(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }

        }

        [HttpPost("get-token-external")]
        public async Task<IActionResult> GetToken([FromBody] ExternalLoginRequestDTO model)
        {

            try
            {
                var tokenResponse = await GetTokenFromCode(model.Token);

                if (tokenResponse == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { "Invalid Google token." };
                    return BadRequest(_response);
                }
                var payload = await _userRepository.VerifyGoogleToken(tokenResponse.IdToken);

                if (payload == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { "Invalid Google token." };
                    return BadRequest(_response);
                }
                if (payload.ExpirationTimeSeconds == 0)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages = new List<string>() { "Token google expired." };
                    return BadRequest(_response);
                }
                var user = await _userRepository.GetUserByEmailAsync(payload.Email);
                var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
                if (user == null)
                {
                    user = await _userRepository.CreateAsync(new ApplicationUser
                    {
                        Email = payload.Email,
                        Role = SD.USER_ROLE,
                        UserType = SD.GOOGLE_USER,
                        ImageUrl = payload.Picture,
                        ImageLocalPathUrl = @"wwwroot\UserImages\" + SD.IMAGE_DEFAULT_AVATAR_NAME,
                        ImageLocalUrl = baseUrl + $"/{SD.URL_IMAGE_USER}/" + SD.IMAGE_DEFAULT_AVATAR_NAME,
                        FullName = payload.Name,
                        EmailConfirmed = true,
                        IsLockedOut = false
                    }, PasswordGenerator.GeneratePassword());

                    if (user == null)
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        _response.ErrorMessages = new List<string>() { "Error while registering" };
                        return BadRequest(_response);
                    }
                    await _userRepository.UpdateAsync(user);
                }

                var tokenDto = await _userRepository.Login(new LoginRequestDTO()
                {
                    Email = user.UserName,
                    Password = user.PasswordHash
                }, false, tokenResponse.RefreshToken);


                if (tokenDto == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { "User is currently locked out." };
                    return BadRequest(_response);
                }

                tokenDto.AccessTokenGoogle = _tokenEncryption.EncryptToken(tokenResponse.AccessToken);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = tokenDto;
                return Ok(_response);
            }
            catch (MissingMemberException e)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.NotAcceptable;
                _response.ErrorMessages = new List<string>() { e.Message };
                return BadRequest(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost("get-new-access-token-external")]
        public async Task<IActionResult> GetNewAccessTokenGoogle([FromBody] string accessTokenGoogle)
        {
            try
            {
                var userInfor = await GetGoogleUserInfoAsync(_tokenEncryption.DecryptToken(accessTokenGoogle));
                var user = await _userRepository.GetUserByEmailAsync(userInfor.Email);
                if (user == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { "Data invalid." };
                    return BadRequest(_response);
                }
                var refreshToken = await _userRepository.GetRefreshTokenGoogleValid(user.Id);
                if (refreshToken == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { "User dont have any refresh token google valid." };
                    return BadRequest(_response);
                }
                var newAccessToken = await GetNewAccessTokenUsingRefreshTokenGoogle(refreshToken);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = _tokenEncryption.EncryptToken(newAccessToken);
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        private async Task<GoogleUserInfo> GetGoogleUserInfoAsync(string accessToken)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo");

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(jsonResponse);
                    return userInfo;
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error retrieving user info: {errorResponse}");
                }
            }
        }

        private async Task<string> GetNewAccessTokenUsingRefreshTokenGoogle(string refreshToken)
        {
            using (var httpClient = new HttpClient())
            {
                var requestBody = new Dictionary<string, string>
                {
                    { "client_id", clientId },
                    { "client_secret", clientSecret },
                    { "refresh_token", refreshToken },
                    { "grant_type", "refresh_token" }
                };

                var requestContent = new FormUrlEncodedContent(requestBody);

                var response = await httpClient.PostAsync("https://oauth2.googleapis.com/token", requestContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonSerializer.Deserialize<GoogleTokenResponse>(responseContent);
                    return tokenResponse.AccessToken;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error refreshing token: {errorContent}");
                }
            }
        }

        private async Task<GoogleTokenResponse> GetTokenFromCode(string code)
        {
            try
            {
                var tokenRequestUri = "https://oauth2.googleapis.com/token";
                var client = new HttpClient();
                var requestBody = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("redirect_uri", SD.URL_FE),
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                });

                // Send the request
                var response = await client.PostAsync(tokenRequestUri, requestBody);

                // Log the request and response
                var requestContent = await requestBody.ReadAsStringAsync();
                Console.WriteLine("Request Content: " + requestContent);

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Response Content: " + responseContent);
                var tokenResponse = JsonSerializer.Deserialize<GoogleTokenResponse>(responseContent);
                return tokenResponse;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

    }
}
