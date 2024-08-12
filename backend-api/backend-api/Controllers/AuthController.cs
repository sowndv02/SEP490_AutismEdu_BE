using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace backend_api.Controllers
{
    [Route("api/v{version:apiVersion}/Auth")]
    [ApiController]
    [ApiVersionNeutral]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        private string audience = string.Empty;
        public AuthController(IUserRepository userRepository, IMapper mapper, IConfiguration configuration)
        {
            _mapper = mapper;
            _userRepository = userRepository;
            _response = new();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDTO model)
        {
            var tokenDto = await _userRepository.Login(model);
            if (tokenDto == null)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("User is currently locked out.");
                return BadRequest(_response);
            }
            if (string.IsNullOrEmpty(tokenDto.AccessToken))
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Username or password is incorrect");
                return BadRequest(_response);
            }
            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            _response.Result = tokenDto;
            return Ok(_response);
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterationRequestDTO model)
        {
            bool ifUserNameUnique = _userRepository.IsUniqueUser(model.UserName);
            if (!ifUserNameUnique)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Username already exists");
                return BadRequest(_response);
            }

            var user = await _userRepository.Register(model);
            if (user == null)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages.Add("Error while registering");
                return BadRequest(_response);
            }
            var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
            user.ImageUrl = baseUrl + $"/{SD.UrlImageUser}/" + SD.UrlImageAvatarDefault;
            user.ImageLocalPathUrl = @"wwwroot\UserImages\" + SD.UrlImageAvatarDefault;
            user.CreatedDate = DateTime.Now;
            await _userRepository.UpdateAsync(user);
            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            return Ok(_response);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> GetNewTokenFromRefreshToken([FromBody] TokenDTO model)
        {
            if (ModelState.IsValid)
            {
                var tokenDTOResponse = await _userRepository.RefreshAccessToken(model);
                if (tokenDTOResponse == null || string.IsNullOrEmpty(tokenDTOResponse.AccessToken))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Token Invalid");
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
                _response.ErrorMessages.Add("Error while refresh token");
                return BadRequest(_response);
            }
        }

        [HttpPost("revoke")]
        public async Task<IActionResult> RevokeRefreshToken([FromBody] TokenDTO model)
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
            _response.Result = "Invalid Input";
            return BadRequest(_response);

        }

        


    }
}
