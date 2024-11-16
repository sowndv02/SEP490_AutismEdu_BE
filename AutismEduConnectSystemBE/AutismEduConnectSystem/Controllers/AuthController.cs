using AutoMapper;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Models.DTOs;
using AutismEduConnectSystem.RabbitMQSender;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.Utils;
using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace AutismEduConnectSystem.Controllers
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
        private readonly FormatString _formatString;
        private readonly IRabbitMQMessageSender _messageBus;
        private static int validateTime = 0;
        private static string clientId = string.Empty;
        private static string queueName = string.Empty;
        private static string clientSecret = string.Empty;
        private readonly ILogger<AuthController> _logger;
        private readonly IResourceService _resourceService;

        public AuthController(IUserRepository userRepository, IMapper mapper,
            IConfiguration configuration, IRabbitMQMessageSender messageBus, DateTimeEncryption dateTimeEncryption,
            TokenEcryption tokenEncryption, FormatString formatString, ILogger<AuthController> logger, 
            IResourceService resourceService)
        {
            validateTime = configuration.GetValue<int>("APIConfig:ValidateTime");
            clientId = configuration.GetValue<string>("Authentication:Google:ClientId");
            clientSecret = configuration.GetValue<string>("Authentication:Google:ClientSecret");
            queueName = configuration.GetValue<string>("RabbitMQSettings:QueueName");
            _dateTimeEncryption = dateTimeEncryption;
            _mapper = mapper;
            _userRepository = userRepository;
            _response = new();
            _messageBus = messageBus;
            _tokenEncryption = tokenEncryption;
            _formatString = formatString;
            _logger = logger;
            _resourceService = resourceService; 
        }

        [HttpPost("resend-confirm-email")]
        [AllowAnonymous]
        public async Task<ActionResult<APIResponse>> ResendConfirmEmail(ResendConfirmEmailDTO model)
        {

            try
            {
                if (model == null)
                {
                    _logger.LogWarning("ResendConfirmEmail called with a null model.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.EMAIL) };
                    return BadRequest(_response);
                }
                var user = await _userRepository.GetUserByEmailAsync(model.Email);
                if (user == null)
                {
                    _logger.LogWarning($"User not found with email: {model.Email}");
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.USER) };
                    return BadRequest(_response);
                }
                string code = await _userRepository.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = $"{SD.URL_FE_FULL}/confirm-register?userId={user.Id}&code={code}&security={_dateTimeEncryption.EncryptDateTime(DateTime.Now)}";
                _messageBus.SendMessage(new EmailLogger() { UserId = user.Id, Email = user.Email, Subject = "Xác nhận Email", Message = $"Thời gian hết hạn 5 phút. \nĐể xác nhận email hãy click vào đường dẫn: <a href='{callbackUrl}'>link</a>" }, queueName);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while resending the confirmation email.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPost("confirm-email")]
        [AllowAnonymous]
        public async Task<ActionResult<APIResponse>> ConfirmEmail(ConfirmEmailDTO model)
        {

            try
            {
                if (model == null)
                {
                    _logger.LogWarning("ConfirmEmail called with a null model.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.INFORMATION) };
                    return BadRequest(_response);
                }
                var user = await _userRepository.GetAsync(x => x.Id == model.UserId);
                if (user == null)
                {
                    _logger.LogWarning($"User not found with ID: {model.UserId}");
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.USER) };
                    return BadRequest(_response);
                }
                DateTime security = _dateTimeEncryption.DecryptDateTime(model.Security);
                if (DateTime.Now > security.AddMinutes(validateTime))
                {
                    _logger.LogWarning($"Link expired for user: {model.UserId}. Expiry time: {security.AddMinutes(validateTime)}");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.LINK_EXPIRED_MESSAGE) };
                    return BadRequest(_response);
                }
                var result = await _userRepository.ConfirmEmailAsync(user, model.Code);
                if (!result)
                {
                    _logger.LogError($"Email confirmation failed for user: {model.UserId}. Internal server error.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.InternalServerError;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.InternalServerError, _response);
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while confirming the email.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<ActionResult<APIResponse>> ResetPassword(ResetPasswordDTO model)
        {

            try
            {
                if (model == null)
                {
                    _logger.LogWarning("ResetPassword called with a null model.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.INFORMATION) };
                    return BadRequest(_response);
                }
                var user = await _userRepository.GetAsync(x => x.Id == model.UserId);
                if (user == null)
                {
                    _logger.LogWarning($"User not found with UserId: {model.UserId}");
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.USER) };
                    return BadRequest(_response);
                }
                else if (user.UserType == SD.GOOGLE_USER)
                {
                    _logger.LogWarning($"Google user with UserId: {model.UserId} attempted password reset.");
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { $"User gooogle cannot forgot password." };
                    return NotFound(_response);
                }
                DateTime security = _dateTimeEncryption.DecryptDateTime(model.Security);
                if (DateTime.Now > security.AddMinutes(validateTime))
                {
                    _logger.LogWarning($"Reset password link expired for UserId: {model.UserId}. Expiry time: {security.AddMinutes(validateTime)}");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { "Link Expired." };
                    return BadRequest(_response);
                }
                var result = await _userRepository.ResetPasswordAsync(user, model.Code, model.Password);
                if (!result)
                {
                    _logger.LogError($"Password reset failed for UserId: {model.UserId}. Internal server error.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.InternalServerError;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.InternalServerError, _response);
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while resetting the password.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<ActionResult<APIResponse>> ForgotPassword(ForgotPasswordDTO forgotPasswordDTO)
        {

            try
            {
                if (forgotPasswordDTO == null)
                {
                    _logger.LogWarning("ForgotPassword called with a null model.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { "Data invalid." };
                    return BadRequest(_response);
                }
                var user = await _userRepository.GetUserByEmailAsync(forgotPasswordDTO.Email);
                if (user == null)
                {
                    _logger.LogWarning($"User not found with email: {forgotPasswordDTO.Email}");
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.USER) };
                    return NotFound(_response);
                }
                else if (user.UserType == SD.GOOGLE_USER)
                {
                    _logger.LogWarning($"Google user with email: {forgotPasswordDTO.Email} attempted password reset.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.GOOGLE_USER_INVALID_FORGOT_PASSWORD_MESSAGE) };
                    return NotFound(_response);
                }
                var code = await _userRepository.GeneratePasswordResetTokenAsync(user);

                var callbackUrl = $"{SD.URL_FE_FULL}/reset-password?userId={user.Id}&code={code}&security={_dateTimeEncryption.EncryptDateTime(DateTime.Now)}";

                _messageBus.SendMessage(new EmailLogger()
                {
                    UserId = user.Id,
                    Email = forgotPasswordDTO.Email,
                    Subject = "Đặt lại mật khẩu",
                    Message = $"Thời gian hết hạn 5 phút. \nĐể đặt lại mật khẩu vui lòng click vào đường dẫn này: <a href='{callbackUrl}'>link</a>"
                }, queueName);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the forgot password request.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<APIResponse>> Login(LoginRequestDTO model)
        {
            try
            {
                model.AuthenticationRole = _formatString.FormatStringUpperCaseFirstChar(model.AuthenticationRole);
                var tokenDto = await _userRepository.Login(model);
                if (tokenDto == null)
                {
                    _logger.LogWarning("Login failed for user: {Email}. User is locked out.", model.Email);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.ACCOUNT_IS_LOCK_MESSAGE) };
                    return BadRequest(_response);
                }
                if (string.IsNullOrEmpty(tokenDto.AccessToken))
                {
                    _logger.LogWarning("Login failed for user: {Email}. Incorrect username or password.", model.Email);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.USERNAME_PASSWORD_INVALID_MESSAGE) };
                    return BadRequest(_response);
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = tokenDto;
                return Ok(_response);
            }
            catch (MissingMemberException e)
            {
                _logger.LogError(e, "Missing member exception occurred during login for user: {Email}", model.Email);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.NotAcceptable;
                _response.ErrorMessages = new List<string>() { e.Message };
                return BadRequest(_response);
            }
            catch (InvalidOperationException e)
            {
                _logger.LogError(e, "Missing member exception occurred during login for user: {Email}", model.Email);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.NotAcceptable;
                _response.ErrorMessages = new List<string>() { e.Message };
                return BadRequest(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the login for user: {Email}", model.Email);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPost("register")]
        public async Task<ActionResult<APIResponse>> Register([FromBody] RegisterationRequestDTO model)
        {
            try
            {
                bool ifUserNameUnique = _userRepository.IsUniqueUser(model.Email);
                if (!ifUserNameUnique)
                {
                    _logger.LogWarning("Registration failed. Email already exists: {Email}", model.Email);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.EMAIL_EXISTING_MESSAGE) };
                    return BadRequest(_response);
                }

                var user = await _userRepository.Register(model);
                if (user == null)
                {
                    _logger.LogWarning("Registration failed for email: {Email}. Error while registering.", model.Email);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.REGISTER_FAILED_MESSAGE) };
                    return BadRequest(_response);
                }
                user.ImageUrl = SD.URL_IMAGE_DEFAULT_BLOB;
                user.CreatedDate = DateTime.Now;
                user.UserType = SD.APPLICATION_USER;
                await _userRepository.UpdateAsync(user);

                string code = await _userRepository.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = $"{SD.URL_FE_FULL}/confirm-register?userId={user.Id}&code={code}&security={_dateTimeEncryption.EncryptDateTime(DateTime.Now)}";
                _messageBus.SendMessage(new EmailLogger()
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Subject = "Confirm Email",
                    Message = $"Expiration time 5 minutes. \nPlease confirm email by clicking here: <a href='{callbackUrl}'>link</a>"
                }, queueName);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while registering user with email: {Email}", model.Email);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<APIResponse>> GetNewTokenFromRefreshToken([FromBody] TokenDTO model, [FromRoute] bool isRequiredGoogle = false)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var tokenDTOResponse = await _userRepository.RefreshAccessToken(model);
                    if (tokenDTOResponse == null || string.IsNullOrEmpty(tokenDTOResponse.AccessToken))
                    {
                        _logger.LogWarning("Token refresh failed: Invalid token for refresh request.");
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.APPLICATION_TOKEN) };
                        return BadRequest(_response);
                    }
                    if (isRequiredGoogle && model.AccessTokenGoogle != null)
                    {
                        var payload = await _userRepository.VerifyGoogleToken(_tokenEncryption.DecryptToken(model.AccessTokenGoogle));

                        if (payload == null)
                        {
                            _logger.LogWarning("Invalid Google token provided.");
                            _response.StatusCode = HttpStatusCode.BadRequest;
                            _response.IsSuccess = false;
                            _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.GOOGLE_TOKEN) };
                            return BadRequest(_response);
                        }
                        var user = await _userRepository.GetUserByEmailAsync(payload.Email);
                        if (user == null)
                        {
                            _logger.LogWarning("No user found for Google email: {Email}", payload.Email);
                            _response.StatusCode = HttpStatusCode.BadRequest;
                            _response.IsSuccess = false;
                            _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.GOOGLE_USER) };
                            return BadRequest(_response);
                        }
                        var refreshToken = await _userRepository.GetRefreshTokenGoogleValid(user.Id);
                        if (refreshToken == null)
                        {
                            _logger.LogWarning("No valid refresh token found for Google user: {Email}", user.Email);
                            _response.StatusCode = HttpStatusCode.BadRequest;
                            _response.IsSuccess = false;
                            _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.GOOGLE_REFRESH_TOKEN_STRING) };
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
                    _logger.LogWarning("Model state is invalid for token refresh request.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.REFRESH_TOKEN_ERROR_MESSAGE) };
                    return BadRequest(_response);
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "An error occurred while trying to generate a new access token for Google.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the refresh token request.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost("revoke")]
        public async Task<ActionResult<APIResponse>> RevokeRefreshToken([FromBody] TokenDTO model)
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
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.INFORMATION) };
                return BadRequest(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the revoke refresh token request.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost("external-login")]
        [AllowAnonymous]
        public async Task<ActionResult<APIResponse>> ExternalLogin([FromBody] ExternalLoginRequestDTO model)
        {
            try
            {
                var payload = await _userRepository.VerifyGoogleToken(model.Token);

                if (payload == null)
                {
                    _logger.LogWarning("Invalid Google token received: {Token}", model?.Token);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.GOOGLE_TOKEN) };
                    return BadRequest(_response);
                }
                if (payload.ExpirationTimeSeconds == 0)
                {
                    _logger.LogWarning("Google token expired for token: {Token}", model?.Token);
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.TOKEN_EXPIRED_MESSAGE) };
                    return BadRequest(_response);
                }
                var user = await _userRepository.GetUserByEmailAsync(payload.Email);
                if (user == null)
                {
                    user = await _userRepository.CreateAsync(new ApplicationUser
                    {
                        Email = payload.Email,
                        Role = SD.PARENT_ROLE,
                        UserType = SD.GOOGLE_USER,
                        ImageUrl = payload.Picture,
                        FullName = payload.Name,
                        EmailConfirmed = true,
                        IsLockedOut = false
                    }, PasswordGenerator.GeneratePassword());

                    if (user == null)
                    {
                        _logger.LogError("Error while creating user with email {Email}.", payload.Email);
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
                    _logger.LogWarning("User login failed. User is locked out or invalid credentials for email: {Email}", user.Email);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.ACCOUNT_IS_LOCK_MESSAGE) };
                    return BadRequest(_response);
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = tokenDto;
                return Ok(_response);
            }
            catch (MissingMemberException e)
            {
                _logger.LogError(e, "Missing member exception occurred during external login.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.NotAcceptable;
                _response.ErrorMessages = new List<string>() { e.Message };
                return BadRequest(_response);
            }
            catch (InvalidOperationException e)
            {
                _logger.LogError(e, "User locked");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.NotAcceptable;
                _response.ErrorMessages = new List<string>() { e.Message };
                return BadRequest(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the external login request.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }

        }

        [HttpPost("get-token-external")]
        public async Task<ActionResult<APIResponse>> GetToken([FromBody] ExternalLoginRequestDTO model)
        {

            try
            {
                var tokenResponse = await GetTokenFromCode(model.Token);

                if (tokenResponse == null)
                {
                    _logger.LogWarning("Invalid Google token received: {Token}", model?.Token);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.GOOGLE_TOKEN) };
                    return BadRequest(_response);
                }
                var payload = await _userRepository.VerifyGoogleToken(tokenResponse.IdToken);

                if (payload == null)
                {
                    _logger.LogWarning("Invalid Google ID token: {IdToken}", tokenResponse?.IdToken);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.GOOGLE_TOKEN) };
                    return BadRequest(_response);
                }
                if (payload.ExpirationTimeSeconds == 0)
                {
                    _logger.LogWarning("Google token expired for token: {IdToken}", tokenResponse?.IdToken);
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.TOKEN_EXPIRED_MESSAGE) };
                    return BadRequest(_response);
                }
                var user = await _userRepository.GetUserByEmailAsync(payload.Email);
                if (user == null)
                {
                    user = await _userRepository.CreateAsync(new ApplicationUser
                    {
                        Email = payload.Email,
                        Role = SD.PARENT_ROLE,
                        UserType = SD.GOOGLE_USER,
                        ImageUrl = payload.Picture,
                        FullName = payload.Name,
                        EmailConfirmed = true,
                        IsLockedOut = false
                    }, PasswordGenerator.GeneratePassword());

                    if (user == null)
                    {
                        _logger.LogError("Error while creating user with email {Email}.", payload.Email);
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.REGISTER_FAILED_MESSAGE) };
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
                    _logger.LogWarning("User login failed. User is locked out or invalid credentials for email: {Email}", user.Email);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.ACCOUNT_IS_LOCK_MESSAGE) };
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
                _logger.LogError(e, "Missing member exception occurred during token exchange process.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.NotAcceptable;
                _response.ErrorMessages = new List<string>() { e.Message };
                return BadRequest(_response);
            }
            catch (InvalidOperationException e)
            {
                _logger.LogError(e, "Missing member exception occurred during login for user");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.NotAcceptable;
                _response.ErrorMessages = new List<string>() { e.Message };
                return BadRequest(_response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "An error occurred while trying to generate a new access token for Google.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the external login token request.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost("get-new-access-token-external")]
        public async Task<ActionResult<APIResponse>> GetNewAccessTokenGoogle([FromBody] string accessTokenGoogle)
        {
            try
            {
                var userInfor = await GetGoogleUserInfoAsync(_tokenEncryption.DecryptToken(accessTokenGoogle));
                var user = await _userRepository.GetUserByEmailAsync(userInfor.Email);
                if (user == null)
                {
                    _logger.LogWarning("No user found with email: {Email}. Invalid data.", userInfor.Email);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.INFORMATION) };
                    return BadRequest(_response);
                }
                var refreshToken = await _userRepository.GetRefreshTokenGoogleValid(user.Id);
                if (refreshToken == null)
                {
                    _logger.LogWarning("No valid refresh token found for user with ID: {UserId}", user.Id);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.REFRESH_TOKEN_ERROR_MESSAGE) };
                    return BadRequest(_response);
                }
                var newAccessToken = await GetNewAccessTokenUsingRefreshTokenGoogle(refreshToken);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = _tokenEncryption.EncryptToken(newAccessToken);
                return Ok(_response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "An error occurred while trying to generate a new access token for Google.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while trying to generate a new access token for Google.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        private async Task<GoogleUserInfo> GetGoogleUserInfoAsync(string accessToken)
        {
            try
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
                        _logger.LogError("Failed to retrieve Google user info. Status code: {StatusCode}. Error: {ErrorResponse}",
                                      response.StatusCode, errorResponse);
                        throw new ArgumentException("Đã xảy ra lỗi khi lấy thông tin người dùng từ Google");
                    }
                }
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "An error occurred while retrieving Google user info.");
                throw new ArgumentException("Đã xảy ra lỗi khi lấy thông tin người dùng từ Google");
            } 
        }

        private async Task<string> GetNewAccessTokenUsingRefreshTokenGoogle(string refreshToken)
        {
            try
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

                        _logger.LogError("Failed to refresh Google access token. Status code: {StatusCode}. Error: {ErrorContent}",
                                          response.StatusCode, errorContent);
                        throw new ArgumentException($"Đã xảy ra lỗi khi lấy accesstoken từ refresh token từ Google");
                    }
                }
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "An error occurred while refreshing Google access token.");
                throw new ArgumentException($"Đã xảy ra lỗi khi lấy accesstoken từ refresh token từ Google");
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

                if (response.IsSuccessStatusCode)
                {
                    var tokenResponse = JsonSerializer.Deserialize<GoogleTokenResponse>(responseContent);
                    return tokenResponse;
                }
                else
                {
                    _logger.LogError("Failed to get token from Google. Status Code: {StatusCode}, Response: {ResponseContent}",
                                      response.StatusCode, responseContent);
                    throw new ArgumentException($" Đã xảy ra lỗi khi lấy thông tin token từ Google");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while exchanging authorization code for Google token.");
                throw new ArgumentException($" Đã xảy ra lỗi khi lấy thông tin token từ Google");
            }
        }

    }
}
