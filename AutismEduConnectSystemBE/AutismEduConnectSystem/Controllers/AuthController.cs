﻿using AutismEduConnectSystem.DTOs;
using AutismEduConnectSystem.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.Utils;
using AutoMapper;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
using Azure.Core;

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
        private readonly IEmailSender _messageBus;
        private static int validateTime = 0;
        private static string clientId = string.Empty;
        private static string queueName = string.Empty;
        private static string clientSecret = string.Empty;
        private readonly ILogger<AuthController> _logger;
        private readonly IResourceService _resourceService;

        public AuthController(IUserRepository userRepository, IMapper mapper,
            IConfiguration configuration, IEmailSender messageBus, DateTimeEncryption dateTimeEncryption,
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
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.EMAIL) };
                    return BadRequest(_response);
                }
                var user = await _userRepository.GetUserByEmailAsync(model.Email);
                if (user == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.USER) };
                    return NotFound(_response);
                }
                string code = await _userRepository.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = $"{string.Concat(SD.URL_FE, SD.URL_FE_FULL)}/confirm-register?userId={user.Id}&code={code}&security={_dateTimeEncryption.EncryptDateTime(DateTime.Now)}";
                //_messageBus.SendMessage(new EmailLogger() 
                //{ 
                //    UserId = user.Id, 
                //    Email = user.Email, 
                //    Subject = "Xác nhận Email", 
                //    Message = $"Thời gian hết hạn 5 phút. \nĐể xác nhận email hãy click vào đường dẫn: <a href='{callbackUrl}'>link</a>" 
                //}, queueName);
                await _messageBus.SendEmailAsync(user.Email, "Xác nhận Email", $"Thời gian hết hạn 5 phút. \nĐể xác nhận email hãy click vào đường dẫn: <a href='{callbackUrl}'>link</a>");
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
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
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.INFORMATION) };
                    return BadRequest(_response);
                }
                var user = await _userRepository.GetAsync(x => x.Id == model.UserId);
                if (user == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.USER) };
                    return NotFound(_response);
                }
                DateTime security = _dateTimeEncryption.DecryptDateTime(model.Security);
                if (DateTime.Now > security.AddMinutes(validateTime))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.LINK_EXPIRED_MESSAGE) };
                    return BadRequest(_response);
                }
                var result = await _userRepository.ConfirmEmailAsync(user, model.Code);
                if (!result)
                {
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
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.INFORMATION) };
                    return BadRequest(_response);
                }
                var user = await _userRepository.GetAsync(x => x.Id == model.UserId);
                if (user == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.USER) };
                    return NotFound(_response);
                }
                else if (user.UserType == SD.GOOGLE_USER)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { $"Người dùng Google không thể sử dụng chức năng quên mật khẩu." };
                    return NotFound(_response);
                }
                DateTime security = _dateTimeEncryption.DecryptDateTime(model.Security);
                if (DateTime.Now > security.AddMinutes(validateTime))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.LINK_EXPIRED_MESSAGE) };
                    return BadRequest(_response);
                }
                var result = await _userRepository.ResetPasswordAsync(user, model.Code, model.Password);
                if (!result)
                {
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
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.USER) };
                    return NotFound(_response);
                }
                else if (user.UserType == SD.GOOGLE_USER)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.GOOGLE_USER_INVALID_FORGOT_PASSWORD_MESSAGE) };
                    return NotFound(_response);
                }
                var code = await _userRepository.GeneratePasswordResetTokenAsync(user);

                var callbackUrl = $"{string.Concat(SD.URL_FE, SD.URL_FE_FULL)}/reset-password?userId={user.Id}&code={code}&security={_dateTimeEncryption.EncryptDateTime(DateTime.Now)}";

                //_messageBus.SendMessage(new EmailLogger()
                //{
                //    UserId = user.Id,
                //    Email = forgotPasswordDTO.Email,
                //    Subject = "Đặt lại mật khẩu",
                //    Message = $"Thời gian hết hạn 5 phút. \nĐể đặt lại mật khẩu vui lòng click vào đường dẫn này: <a href='{callbackUrl}'>link</a>"
                //}, queueName);

                await _messageBus.SendEmailAsync(forgotPasswordDTO.Email, "Đặt lại mật khẩu", $"Thời gian hết hạn 5 phút. \nĐể đặt lại mật khẩu vui lòng click vào đường dẫn này: <a href='{callbackUrl}'>link</a>");

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
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
                if (tokenDto != null && string.IsNullOrEmpty(tokenDto.AccessToken))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.USERNAME_PASSWORD_INVALID_MESSAGE) };
                    return BadRequest(_response);
                }
                if (tokenDto == null)
                {
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
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.NotAcceptable;
                _response.ErrorMessages = new List<string>() { e.Message };
                return BadRequest(_response);
            }
            catch (InvalidOperationException e)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.Locked;
                _response.ErrorMessages = new List<string>() { e.Message };
                return BadRequest(_response);
            }
            catch (InvalidJwtException e)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.NotModified;
                _response.ErrorMessages = new List<string>() { e.Message };
                return BadRequest(_response);
            }
            catch (Exception ex)
            {
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
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.EMAIL_EXISTING_MESSAGE) };
                    return BadRequest(_response);
                }

                var user = await _userRepository.Register(model);
                if (user == null)
                {
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
                var callbackUrl = $"{string.Concat(SD.URL_FE, SD.URL_FE_FULL)}/confirm-register?userId={user.Id}&code={code}&security={_dateTimeEncryption.EncryptDateTime(DateTime.Now)}";
                //_messageBus.SendMessage(new EmailLogger()
                //{
                //    UserId = user.Id,
                //    Email = user.Email,
                //    Subject = "Xác nhận email",
                //    Message = $"Thời gian hết hạn 5 phút. \nĐể xác nhận email hãy click vào đường dẫn: <a href='{callbackUrl}'>link</a>"
                //}, queueName);
                await _messageBus.SendEmailAsync(user.Email, "Xác nhận email", $"Thời gian hết hạn 5 phút. \nĐể xác nhận email hãy click vào đường dẫn: <a href='{callbackUrl}'>link</a>");
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
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
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.REFRESH_TOKEN_ERROR_MESSAGE) };
                    return BadRequest(_response);
                }
            }
            catch (ArgumentException ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
            catch (Exception ex)
            {
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
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.GOOGLE_TOKEN) };
                    return BadRequest(_response);
                }
                if (payload.ExpirationTimeSeconds == 0)
                {
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
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.GOOGLE_TOKEN) };
                    return BadRequest(_response);
                }
                var payload = await _userRepository.VerifyGoogleToken(tokenResponse.IdToken);

                if (payload == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.GOOGLE_TOKEN) };
                    return BadRequest(_response);
                }
                if (payload.ExpirationTimeSeconds == 0)
                {
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
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.NotAcceptable;
                _response.ErrorMessages = new List<string>() { e.Message };
                return BadRequest(_response);
            }
            catch (InvalidOperationException e)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.NotAcceptable;
                _response.ErrorMessages = new List<string>() { e.Message };
                return BadRequest(_response);
            }
            catch (ArgumentException ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
            catch (Exception ex)
            {
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
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
            catch (Exception ex)
            {
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
                        throw new ArgumentException("Đã xảy ra lỗi khi lấy thông tin người dùng từ Google");
                    }
                }
            }
            catch (Exception ex)
            {
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
                    throw new ArgumentException($" Đã xảy ra lỗi khi lấy thông tin token từ Google");
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($" Đã xảy ra lỗi khi lấy thông tin token từ Google");
            }
        }

    }
}
