﻿using AutoMapper;
using Azure;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models.DTOs.UpdateDTOs;
using backend_api.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace backend_api.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IBlobStorageRepository _blobStorageRepository;
        private readonly ILogger<UserController> _logger;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        protected int pageSize = 0;
        public UserController(IUserRepository userRepository, IMapper mapper,
            IConfiguration configuration, IBlobStorageRepository blobStorageRepository,
            ILogger<UserController> logger, IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _mapper = mapper;
            _userRepository = userRepository;
            _response = new();
            _blobStorageRepository = blobStorageRepository;
            _logger = logger;
        }

        [HttpPut("password/{id}", Name = "UpdatePassword")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetPasswordByIdAsync(string id, [FromBody] UpdatePasswordRequestDTO updatePasswordRequestDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(id) || userId != id || userId != updatePasswordRequestDTO.Id)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { $"{id} is invalid!" };
                    return BadRequest(_response);
                }
                await _userRepository.UpdatePasswordAsync(updatePasswordRequestDTO);
                _response.IsSuccess = true;
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpDelete("role/{userId}")]
        public async Task<ActionResult<APIResponse>> RemoveRoleByUserId(string userId, UserRoleDTO userRoleDTO)
        {
            try
            {
                if (!userId.Equals(userRoleDTO.UserId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }
                var result = await _userRepository.RemoveRoleByUserId(userId, userRoleDTO.UserRoleIds);
                if (!result)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.InternalServerError;
                    _response.ErrorMessages = new List<string>() { SD.INTERNAL_SERVER_ERROR_MESSAGE };
                    return StatusCode((int)HttpStatusCode.InternalServerError, _response);
                }
                else
                {
                    _response.IsSuccess = true;
                    _response.StatusCode = HttpStatusCode.NoContent;
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

        [HttpPost("role/{userId}")]
        public async Task<ActionResult<APIResponse>> AddRoleToUser(string userId, UserRoleDTO userRoleDTO)
        {
            try
            {
                if (!userId.Equals(userRoleDTO.UserId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }
                // Get current roles of the user
                var currentRoles = await _userRepository.GetRoleByUserId(userId);

                // Define restricted roles combinations
                var restrictedRoles = new Dictionary<string, List<string>>
                {
                    { SD.STAFF_ROLE, new List<string> { SD.ADMIN_ROLE, SD.TUTOR_ROLE } },
                    { SD.TUTOR_ROLE, new List<string> { SD.ADMIN_ROLE, SD.STAFF_ROLE } },
                    { SD.ADMIN_ROLE, new List<string> { SD.TUTOR_ROLE, SD.STAFF_ROLE } }
                };

                // Validate if any restricted roles are being added
                foreach (var role in currentRoles.Select(x => x.Name))
                {
                    if (restrictedRoles.ContainsKey(role))
                    {
                        foreach (var restrictedRole in restrictedRoles[role])
                        {
                            if (userRoleDTO.UserRoleIds.Contains(restrictedRole))
                            {
                                _response.StatusCode = HttpStatusCode.BadRequest;
                                _response.IsSuccess = false;
                                _response.ErrorMessages = new List<string> { $"Không thể thêm {restrictedRole} cho người dùng với vai trò {role}." };
                                return BadRequest(_response);
                            }
                        }
                    }
                }

                var result = await _userRepository.AddRoleToUser(userId, userRoleDTO.UserRoleIds);
                if (!result)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.InternalServerError;
                    _response.ErrorMessages = new List<string>() { SD.INTERNAL_SERVER_ERROR_MESSAGE };
                    return StatusCode((int)HttpStatusCode.InternalServerError, _response);
                }
                else
                {
                    var responseList = new List<RoleDTO>();
                    foreach (var item in userRoleDTO.UserRoleIds)
                    {
                        IdentityRole model = await _roleRepository.GetByIdAsync(item);
                        responseList.Add(_mapper.Map<RoleDTO>(model));
                    }
                    _response.IsSuccess = true;
                    _response.Result = responseList;
                    _response.StatusCode = HttpStatusCode.Created;
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


        [HttpDelete("claim/{userId}")]
        public async Task<ActionResult<APIResponse>> RemoveClaimByUserId(string userId, UserClaimDTO userClaimDTO)
        {
            try
            {
                if (!userId.Equals(userClaimDTO.UserId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }
                var result = await _userRepository.RemoveClaimByUserId(userId, userClaimDTO.UserClaimIds);
                if (!result)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.InternalServerError;
                    _response.ErrorMessages = new List<string>() { SD.INTERNAL_SERVER_ERROR_MESSAGE };
                    return StatusCode((int)HttpStatusCode.InternalServerError, _response);
                }
                else
                {
                    _response.IsSuccess = true;
                    _response.StatusCode = HttpStatusCode.NoContent;
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

        [HttpPost("claim/{userId}")]
        public async Task<ActionResult<APIResponse>> AddClaimToUser(string userId, UserClaimDTO userClaimDTO)
        {
            try
            {
                if (!userId.Equals(userClaimDTO.UserId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }
                var result = await _userRepository.AddClaimToUser(userId, userClaimDTO.UserClaimIds);
                if (!result)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.InternalServerError;
                    _response.ErrorMessages = new List<string>() { SD.INTERNAL_SERVER_ERROR_MESSAGE };
                    return StatusCode((int)HttpStatusCode.InternalServerError, _response);
                }
                else
                {
                    _response.IsSuccess = true;
                    _response.Result = await _userRepository.GetAsync(x => x.Id == userId);
                    _response.StatusCode = HttpStatusCode.Created;
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


        [HttpPost]
        public async Task<ActionResult<APIResponse>> CreateAsync([FromBody] UserCreateDTO createDTO)
        {
            try
            {
                if (createDTO == null) return BadRequest(createDTO);
                var currentUser = await _userRepository.GetUserByEmailAsync(createDTO.Email);
                if (currentUser != null)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages = new List<string>() { SD.DUPLICATED_MESSAGE };
                    return StatusCode((int)HttpStatusCode.InternalServerError, _response);
                }
                ApplicationUser model = _mapper.Map<ApplicationUser>(createDTO);
                model.CreatedDate = DateTime.Now;
                model.ImageUrl = SD.URL_IMAGE_DEFAULT_BLOB;

                model.UserName = model.Email;
                model.UserType = SD.APPLICATION_USER;
                model.LockoutEnabled = true;
                model.IsLockedOut = false;
                model.EmailConfirmed = true;
                var user = await _userRepository.CreateAsync(model, createDTO.Password);
                if (user == null)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.InternalServerError;
                    _response.ErrorMessages = new List<string>() { SD.INTERNAL_SERVER_ERROR_MESSAGE };
                    return StatusCode((int)HttpStatusCode.InternalServerError, _response);
                }
                else
                {
                    _response.Result = _mapper.Map<ApplicationUserDTO>(user);
                    _response.StatusCode = HttpStatusCode.Created;
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


        [HttpGet("claim/{id}")]
        public async Task<ActionResult<APIResponse>> GetClaimByUserId(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { $"{id} is invalid!" };
                    return BadRequest(_response);
                }
                var exsitingUserClaims = await _userRepository.GetClaimByUserIdAsync(id);
                //_response.Result = _mapper.Map<List<ClaimDTO>>(exsitingUserClaims);
                _response.Result = exsitingUserClaims;
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

        [HttpPut("{id}")]
        public async Task<ActionResult<APIResponse>> UpdateUserAsync(string id, [FromForm] UserUpdateDTO updateDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (updateDTO == null || !id.Equals(userId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }

                var model = await _userRepository.GetAsync(x => x.Id == userId);
                if (updateDTO.Image != null)
                {
                    using var stream = updateDTO.Image.OpenReadStream();
                    model.ImageUrl = await _blobStorageRepository.Upload(stream, string.Concat(Guid.NewGuid().ToString(), Path.GetExtension(updateDTO.Image.FileName)));
                }
                if (!string.IsNullOrEmpty(updateDTO.Address))
                    model.Address = updateDTO.Address;
                if (!string.IsNullOrEmpty(updateDTO.PhoneNumber))
                    model.PhoneNumber = updateDTO.PhoneNumber;
                if (!string.IsNullOrEmpty(updateDTO.FullName))
                    model.FullName = updateDTO.FullName;
                await _userRepository.UpdateAsync(model);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                _response.Result = model;
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


        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? searchValue, string? searchType, string? searchTypeId, int pageNumber = 1)
        {
            try
            {
                int totalCount = 0;
                List<ApplicationUser> list = new();
                bool isAdmin = true;
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                // TODO: HANDLE FOR ROLE
                if (userRoles.Contains(SD.STAFF_ROLE))
                {
                    isAdmin = false;
                }
                if (!string.IsNullOrEmpty(searchType))
                {
                    switch (searchType.ToLower().Trim())
                    {
                        case "all":
                            if (!string.IsNullOrEmpty(searchValue))
                            {
                                var (totalResult, resultObject) = await _userRepository.GetAllAsync(u => (u.Email.ToLower().Contains(searchValue.ToLower())) || (!string.IsNullOrEmpty(u.FullName) && u.FullName.ToLower().Contains(searchValue.ToLower())), pageSize: pageSize, pageNumber: pageNumber, orderBy: x => x.CreatedDate, isDesc: true, isAdminRole: isAdmin);
                                totalCount = totalResult;
                                list = resultObject;
                            }
                            else
                            {
                                var (totalResult, resultObject) = await _userRepository.GetAllAsync(null, pageSize: pageSize, pageNumber: pageNumber, orderBy: x => x.CreatedDate, isDesc: true, isAdminRole: isAdmin);
                                totalCount = totalResult;
                                list = resultObject;
                            }
                            break;
                        case "claim":
                            if (!string.IsNullOrEmpty(searchTypeId))
                            {
                                var (totalResult, users) = await _userRepository.GetUsersForClaimAsync(int.Parse(searchTypeId), pageSize, pageNumber);
                                list = users;
                                totalCount = totalResult;
                            }
                            break;
                        case "role":
                            if (!string.IsNullOrEmpty(searchTypeId))
                            {
                                var (totalResult, users) = await _userRepository.GetUsersInRole(searchTypeId, pageSize, pageNumber);
                                list = users;
                                totalCount = totalResult;
                            }
                            break;
                        default:
                            var (total, result) = await _userRepository.GetAllAsync(null, pageSize: pageSize, pageNumber: pageNumber, orderBy: x => x.CreatedDate, isDesc: true, isAdminRole: isAdmin);
                            list = result;
                            totalCount = _userRepository.GetTotalUser();
                            break;

                    }
                }
                else
                {
                    var (total, result) = await _userRepository.GetAllAsync(null, pageSize: pageSize, pageNumber: pageNumber, orderBy: x => x.CreatedDate, isDesc: true, isAdminRole: isAdmin);
                    list = result;
                    totalCount = _userRepository.GetTotalUser();
                }

                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = totalCount };
                _response.Result = _mapper.Map<List<ApplicationUserDTO>>(list);
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;
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

        [HttpGet("{id}", Name = "GetUserById")]
        public async Task<ActionResult<APIResponse>> GetByIdAsync(string id)
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
                ApplicationUser model = await _userRepository.GetAsync(x => x.Id == id, false, "TutorProfile");
                _response.Result = _mapper.Map<ApplicationUserDTO>(model);
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

        [HttpGet("email/{email}", Name = "GetUserByEmail")]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> GetByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }
                ApplicationUser model = await _userRepository.GetAsync(x => x.Email == email, false, null);
                if (model.Role.Contains(SD.PARENT_ROLE))
                {
                    _response.Result = _mapper.Map<ApplicationUserDTO>(model);
                    _response.StatusCode = HttpStatusCode.OK;
                    return Ok(_response);
                }
                else
                {
                    _response.StatusCode = HttpStatusCode.OK;
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


        [HttpGet("lock/{userId}")]
        public async Task<IActionResult> LockoutUser(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }
                ApplicationUser model = await _userRepository.LockoutUser(userId);
                _response.Result = _mapper.Map<ApplicationUserDTO>(model);
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

        [HttpGet("unlock/{userId}")]
        public async Task<IActionResult> UnLockoutUser(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }
                ApplicationUser model = await _userRepository.UnlockUser(userId);
                _response.Result = _mapper.Map<ApplicationUserDTO>(model);
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

        [HttpDelete("{id}")]
        public async Task<ActionResult<APIResponse>> DeleteAsync(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id)) return BadRequest();
                var obj = await _userRepository.GetAsync(x => x.Id == id);

                if (obj == null) return NotFound();

                await _userRepository.RemoveAsync(id);
                _response.StatusCode = HttpStatusCode.NoContent;
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



        [HttpGet("role/{userId}", Name = "GetRoleByUserId")]
        public async Task<ActionResult<APIResponse>> GetRoleByUserIdAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return NotFound(_response);
                }
                List<IdentityRole> model = await _userRepository.GetRoleByUserId(userId);
                if (model == null || model.Count == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { $"{userId} không có vai trò nào!" };
                    return BadRequest(_response);
                }
                _response.Result = _mapper.Map<List<RoleDTO>>(model);
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
    }
}
