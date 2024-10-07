using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Repository.IRepository;
using backend_api.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace backend_api.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersionNeutral]
    public class RoleController : ControllerBase
    {
        private readonly IRoleRepository _roleRepository;
        private readonly FormatString _formatString;
        private readonly IUserRepository _userRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        protected int takeValue = 0;
        public RoleController(IRoleRepository roleRepository, IMapper mapper, IUserRepository userRepository,
            IConfiguration configuration, FormatString formatString)
        {
            takeValue = configuration.GetValue<int>("APIConfig:TakeValue");
            _response = new();
            _mapper = mapper;
            _roleRepository = roleRepository;
            _userRepository = userRepository;
            _formatString = formatString;
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllRolesAsync()
        {
            try
            {
                List<IdentityRole> list = await _roleRepository.GetAllAsync();
                var result = _mapper.Map<List<RoleDTO>>(list);
                foreach (var role in result)
                {
                    var (totalCount, users) = await _userRepository.GetUsersInRole(role.Name, takeValue);
                    role.TotalUsersInRole = totalCount;
                    role.Users = _mapper.Map<List<ApplicationUserDTO>>(users);
                }
                _response.Result = result;
                _response.StatusCode = HttpStatusCode.OK;
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

        [HttpGet("{id}", Name = "GetRoleById")]
        public async Task<ActionResult<APIResponse>> GetRoleByIdAsync(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }
                IdentityRole model = await _roleRepository.GetByIdAsync(id);
                if (model == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.NOT_FOUND_MESSAGE };
                    return NotFound(_response);
                }
                var result = _mapper.Map<RoleDTO>(model);

                var (totalCount, users) = await _userRepository.GetUsersInRole(model.Name, takeValue);
                result.TotalUsersInRole = totalCount;
                result.Users = _mapper.Map<List<ApplicationUserDTO>>(users);
                _response.Result = result;
                _response.StatusCode = HttpStatusCode.OK;
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

        [HttpPost]
        public async Task<ActionResult<APIResponse>> CreateRoleAsync(RoleCreateDTO roleDTO)
        {
            try
            {
                if (await _roleRepository.GetByNameAsync(roleDTO.Name.Trim()) != null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { $"{roleDTO.Name} tồn tại!" };
                    return BadRequest(_response);
                }
                if (roleDTO == null) return BadRequest(roleDTO);
                roleDTO.Name = _formatString.FormatStringUpperCaseFirstChar(roleDTO.Name);
                IdentityRole model = _mapper.Map<IdentityRole>(roleDTO);
                await _roleRepository.CreateAsync(model);
                _response.Result = _mapper.Map<RoleDTO>(model);
                _response.StatusCode = HttpStatusCode.Created;
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

        [HttpDelete("{roleId}")]
        public async Task<IActionResult> DeleteRoleAsync(string roleId)
        {

            try
            {
                var role = await _roleRepository.GetByIdAsync(roleId);
                if (role != null)
                {
                    bool result = await _roleRepository.RemoveAsync(role);
                    if (result)
                    {
                        _response.IsSuccess = true;
                        _response.StatusCode = HttpStatusCode.NoContent;
                        return Ok(_response);
                    }
                    else
                    {
                        _response.IsSuccess = false;
                        _response.StatusCode = HttpStatusCode.NotFound;
                        _response.ErrorMessages = new List<string>() { $"Không thể xóa vai trò này vì có những người dùng được chỉ định cho vai trò này với ID vai trò là: {roleId}" };
                        return Ok(_response);
                    }
                }
                else
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.ErrorMessages = new List<string>() { SD.NOT_FOUND_MESSAGE };
                    return Ok(_response);
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


    }
}
