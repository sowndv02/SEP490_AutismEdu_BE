using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Models.DTOs;
using AutismEduConnectSystem.Models.DTOs.CreateDTOs;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.Utils;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace AutismEduConnectSystem.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersionNeutral]
    public class RoleController : ControllerBase
    {
        private readonly IRoleRepository _roleRepository;
        private readonly FormatString _formatString;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<RoleController> _logger;
        private readonly IResourceService _resourceService;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        protected int takeValue = 0;
        public RoleController(IRoleRepository roleRepository, IMapper mapper, IUserRepository userRepository,
            IConfiguration configuration, FormatString formatString, ILogger<RoleController> logger,
            IResourceService resourceService)
        {
            takeValue = configuration.GetValue<int>("APIConfig:TakeValue");
            _response = new();
            _mapper = mapper;
            _roleRepository = roleRepository;
            _userRepository = userRepository;
            _formatString = formatString;
            _logger = logger;
            _resourceService = resourceService;
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
                _logger.LogError(ex, "Error occurred in GetAllRolesAsync");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
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
                    _logger.LogWarning("Invalid Role ID provided: {RoleId}", id);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }
                IdentityRole model = await _roleRepository.GetByIdAsync(id);
                if (model == null)
                {
                    _logger.LogWarning("Role with ID: {RoleId} not found", id);
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
                _logger.LogError(ex, "Error occurred while retrieving role with ID: {RoleId}", id);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost]
        [Authorize(Roles = SD.ADMIN_ROLE)]
        public async Task<ActionResult<APIResponse>> CreateRoleAsync(RoleCreateDTO roleDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                if (userRoles == null || (!userRoles.Contains(SD.ADMIN_ROLE)))
                {
                    _logger.LogWarning("Forbidden access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                if (await _roleRepository.GetByNameAsync(roleDTO.Name.Trim()) != null)
                {
                    _logger.LogWarning("Role with name {RoleName} already exists", roleDTO.Name);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { $"{roleDTO.Name} tồn tại!" };
                    return BadRequest(_response);
                }
                if (roleDTO == null)
                {
                    _logger.LogWarning("RoleDTO is null, returning BadRequest");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ROLE) };
                    return BadRequest(_response);
                }
                roleDTO.Name = _formatString.FormatStringUpperCaseFirstChar(roleDTO.Name);
                IdentityRole model = _mapper.Map<IdentityRole>(roleDTO);
                await _roleRepository.CreateAsync(model);
                _response.Result = _mapper.Map<RoleDTO>(model);
                _response.StatusCode = HttpStatusCode.Created;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating role with name: {RoleName}", roleDTO?.Name);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
