using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Repository.IRepository;
using backend_api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;

namespace backend_api.Controllers
{
    [Route("api/v{version:apiVersion}/Auth")]
    [ApiController]
    [ApiVersionNeutral]
    public class AuthController : ControllerBase
    {
        private readonly DateTimeEncryption _dateTimeEncryption;
        private readonly IUserRepository _userRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        private string audience = string.Empty;
        private readonly IEmailSender _emailSender;
        private static int ValidateTime = 0;
        public AuthController(IUserRepository userRepository, IMapper mapper, 
            IConfiguration configuration, IEmailSender emailSender, DateTimeEncryption dateTimeEncryption)
        {
            ValidateTime = configuration.GetValue<int>("APIConfig:ValidateTime");
            _dateTimeEncryption = dateTimeEncryption;
            _mapper = mapper;
            _userRepository = userRepository;
            _response = new();
            _emailSender = emailSender;
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
                if(security > security.AddMinutes(ValidateTime))
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
                DateTime security = _dateTimeEncryption.DecryptDateTime(model.Security);
                if (security > security.AddMinutes(ValidateTime))
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
                user.ImageLocalUrl = baseUrl + $"/{SD.UrlImageUser}/" + SD.UrlImageAvatarDefault;
                user.ImageUrl = SD.URL_IMAGE_DEFAULT_BLOB;
                user.ImageLocalPathUrl = @"wwwroot\UserImages\" + SD.UrlImageAvatarDefault;
                user.CreatedDate = DateTime.Now;
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
        public async Task<IActionResult> GetNewTokenFromRefreshToken([FromBody] TokenDTO model)
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




    }
}
