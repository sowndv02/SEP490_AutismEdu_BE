using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Models.DTOs;
using AutismEduConnectSystem.Models.DTOs.CreateDTOs;
using AutismEduConnectSystem.Models.DTOs.UpdateDTOs;
using AutismEduConnectSystem.Repository;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.Utils;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using static AutismEduConnectSystem.SD;

namespace AutismEduConnectSystem.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersionNeutral]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly ITutorRepository _tutorRepository;
        private readonly IBlobStorageRepository _blobStorageRepository;
        private readonly ILogger<UserController> _logger;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        protected int pageSize = 0;
        private readonly IResourceService _resourceService;

        public UserController(IUserRepository userRepository, IMapper mapper,
            IConfiguration configuration, IBlobStorageRepository blobStorageRepository,
            ILogger<UserController> logger, IRoleRepository roleRepository, 
            IResourceService resourceService, ITutorRepository tutorRepository)
        {
            _roleRepository = roleRepository;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _mapper = mapper;
            _userRepository = userRepository;
            _response = new();
            _blobStorageRepository = blobStorageRepository;
            _logger = logger;
            _resourceService = resourceService;
            _tutorRepository = tutorRepository;
        }

        [HttpPut("password/{id}", Name = "UpdatePassword")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetPasswordByIdAsync(string id, [FromBody] UpdatePasswordRequestDTO updatePasswordRequestDTO)
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
                if (string.IsNullOrEmpty(id) || userId != id || userId != updatePasswordRequestDTO.Id)
                {
                    _logger.LogWarning("Invalid ID or mismatch. Request ID: {RequestId}, Authenticated User ID: {UserId}", id, userId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                await _userRepository.UpdatePasswordAsync(updatePasswordRequestDTO);
                _response.IsSuccess = true;
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (InvalidDataException ex)
            {
                _logger.LogError(ex, "Error occurred while updating password for User ID: {UserId}", id);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.BadRequest, _response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating password for User ID: {UserId}", id);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpDelete("role/{userId}")]
        [Authorize(Roles = SD.ADMIN_ROLE)]

        public async Task<ActionResult<APIResponse>> RemoveRoleByUserId(string userId, UserRoleDTO userRoleDTO)
        {
            try
            {
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(adminId))
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
                if (!userId.Equals(userRoleDTO.UserId))
                {
                    _logger.LogWarning("User ID mismatch. Request ID: {RequestId}, Provided DTO UserId: {UserRoleDTOUserId}", userId, userRoleDTO.UserId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                var result = await _userRepository.RemoveRoleByUserId(userId, userRoleDTO.UserRoleIds);
                if (!result)
                {
                    _logger.LogError("Failed to remove roles for User ID: {UserId}", userId);
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.InternalServerError;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
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
                _logger.LogError(ex, "Error occurred while removing roles for User ID: {UserId}", userId);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost("role/{userId}")]
        [Authorize(Roles = SD.ADMIN_ROLE)]
        public async Task<ActionResult<APIResponse>> AddRoleToUser(string userId, UserRoleDTO userRoleDTO)
        {
            try
            {
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(adminId))
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
                if (!userId.Equals(userRoleDTO.UserId))
                {
                    _logger.LogWarning("User ID mismatch. Request ID: {RequestId}, Provided DTO UserId: {UserRoleDTOUserId}", userId, userRoleDTO.UserId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
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
                                _logger.LogWarning("Attempt to add restricted role {RestrictedRole} to user with role {CurrentRole}", restrictedRole, role);
                                _response.StatusCode = HttpStatusCode.BadRequest;
                                _response.IsSuccess = false;
                                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.CANNOT_ADD_ROLE, restrictedRole, role) };
                                return BadRequest(_response);
                            }
                        }
                    }
                }

                var result = await _userRepository.AddRoleToUser(userId, userRoleDTO.UserRoleIds);
                if (!result)
                {
                    _logger.LogError("Failed to add roles for User ID: {UserId}", userId);
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.InternalServerError;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
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
            catch (InvalidDataException ex)
            {
                _logger.LogError(ex, "Error occurred while updating password for User ID: {UserId}", id);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.BadRequest, _response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding roles for User ID: {UserId}", userId);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpDelete("claim/{userId}")]
        [Authorize(Roles = SD.ADMIN_ROLE)]
        public async Task<ActionResult<APIResponse>> RemoveClaimByUserId(string userId, UserClaimDTO userClaimDTO)
        {
            try
            {
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(adminId))
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
                if (!userId.Equals(userClaimDTO.UserId))
                {
                    _logger.LogWarning("User ID mismatch. Request ID: {RequestId}, Provided DTO UserId: {UserClaimDTOUserId}", userId, userClaimDTO.UserId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                var result = await _userRepository.RemoveClaimByUserId(userId, userClaimDTO.UserClaimIds);
                if (!result)
                {
                    _logger.LogError("Failed to remove claims for User ID: {UserId}", userId);
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.InternalServerError;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
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
                _logger.LogError(ex, "Error occurred while removing claims for User ID: {UserId}", userId);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPost("claim/{userId}")]
        [Authorize(Roles = SD.ADMIN_ROLE)]
        public async Task<ActionResult<APIResponse>> AddClaimToUser(string userId, UserClaimDTO userClaimDTO)
        {
            try
            {
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(adminId))
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
                if (!userId.Equals(userClaimDTO.UserId))
                {
                    _logger.LogWarning("User ID mismatch. Request ID: {RequestId}, Provided DTO UserId: {UserClaimDTOUserId}", userId, userClaimDTO.UserId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                var result = await _userRepository.AddClaimToUser(userId, userClaimDTO.UserClaimIds);
                if (!result)
                {
                    _logger.LogError("Failed to add claims for User ID: {UserId}", userId);
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.InternalServerError;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
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
                _logger.LogError(ex, "Error occurred while adding claims for User ID: {UserId}", userId);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPost]
        [Authorize]
        public async Task<ActionResult<APIResponse>> CreateAsync([FromBody] UserCreateDTO createDTO)
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
                if (createDTO == null)
                {
                    _logger.LogWarning("Received null DTO for user creation.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.InternalServerError;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.USER) };
                    return BadRequest(_response);
                }
                var currentUser = await _userRepository.GetUserByEmailAsync(createDTO.Email);
                if (currentUser != null)
                {
                    _logger.LogWarning("User with email {Email} already exists.", createDTO.Email);
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.USER) };
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
                    _logger.LogError("Failed to create user with email: {Email}", createDTO.Email);
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.InternalServerError;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
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
                _logger.LogError(ex, "Error occurred while creating user with email: {Email}", createDTO.Email);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
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
                    _logger.LogWarning("Received empty or null user ID for claim retrieval.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
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
                _logger.LogError(ex, "Error occurred while fetching claims for user with ID: {UserId}", id);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<APIResponse>> UpdateUserAsync(string id, [FromForm] UserUpdateDTO updateDTO)
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
                if (updateDTO == null || !id.Equals(userId))
                {
                    _logger.LogWarning("Bad Request: User ID mismatch or empty update data. UserId: {UserId}, RequestId: {RequestId}", userId, id);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }

                var model = await _userRepository.GetAsync(x => x.Id == userId);
                if (model == null)
                {
                    _logger.LogWarning("User not found: UserId: {UserId}", userId);
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.USER) };
                    return NotFound(_response);
                }
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
                _logger.LogError(ex, "Error occurred while updating user information for UserId: {UserId}", id);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
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
                bool isAdmin = false;
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                if (userRoles.Contains(SD.STAFF_ROLE) || userRoles.Contains(SD.MANAGER_ROLE))
                {
                    isAdmin = false;
                }
                Expression<Func<ApplicationUser, bool>> filter = u => true;
                if (!string.IsNullOrEmpty(searchValue))
                {
                    filter = filter.AndAlso(u => u.Email.ToLower().Contains(searchValue.ToLower()) || !string.IsNullOrEmpty(u.FullName) && u.FullName.ToLower().Contains(searchValue.ToLower()));
                }

                if (!string.IsNullOrEmpty(searchType))
                {
                    switch (searchType.ToLower().Trim())
                    {
                        case "all":
                            var (totalResultAll, resultObjectAll) = await _userRepository.GetAllAsync(filter, pageSize: pageSize, pageNumber: pageNumber, orderBy: x => x.CreatedDate, isDesc: true, isAdminRole: isAdmin);
                            totalCount = totalResultAll;
                            list = resultObjectAll;
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
                        case "parent":
                            var (totalParent, resultParent) = await _userRepository.GetAllAsync(filter, pageSize: pageSize, pageNumber: pageNumber, orderBy: x => x.CreatedDate, isDesc: true, isAdminRole: isAdmin, byRole: SD.PARENT_ROLE);
                            totalCount = totalParent;
                            list = resultParent;
                            break;
                        case "tutor":
                            var (totalTutor, resultTutor) = await _userRepository.GetAllAsync(filter, pageSize: pageSize, pageNumber: pageNumber, orderBy: x => x.CreatedDate, isDesc: true, isAdminRole: isAdmin, byRole: SD.TUTOR_ROLE);
                            totalCount = totalTutor;
                            list = resultTutor;
                            break;
                        default:
                            var (total, result) = await _userRepository.GetAllAsync(filter, pageSize: pageSize, pageNumber: pageNumber, orderBy: x => x.CreatedDate, isDesc: true, isAdminRole: isAdmin);
                            list = result;
                            totalCount = _userRepository.GetTotalUser();
                            break;

                    }
                }
                else
                {
                    var (total, result) = await _userRepository.GetAllAsync(filter, pageSize: pageSize, pageNumber: pageNumber, orderBy: x => x.CreatedDate, isDesc: true, isAdminRole: isAdmin);
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
                _logger.LogError(ex, "Error occurred while retrieving users.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        


        [HttpGet("{id}", Name = "GetUserById")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetByIdAsync(string id)
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
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Received a bad request with an empty or null id.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                ApplicationUser model = await _userRepository.GetAsync(x => x.Id == id, false, "TutorProfile");
                if(model.TutorProfile != null)
                {
                    model.TutorProfile = await _tutorRepository.GetAsync(x => x.TutorId == id, false, "User,Curriculums,AvailableTimeSlots,Certificates,WorkExperiences,Reviews");
                }
                if (model == null)
                {
                    _logger.LogWarning("User not found for Id: {UserId}", id);
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.USER) };
                    return NotFound(_response);
                }
                _response.Result = _mapper.Map<ApplicationUserDTO>(model);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving user with Id: {UserId}", id);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet("email/{email}", Name = "GetUserByEmail")]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> GetByEmailAsync(string email)
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
                if (userRoles == null || (!userRoles.Contains(SD.TUTOR_ROLE)))
                {
                    _logger.LogWarning("Forbidden access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Received a bad request with an empty or null email.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.EMAIL) };
                    return BadRequest(_response);
                }
                ApplicationUser model = await _userRepository.GetAsync(x => x.Email == email, false, null);

                if (model == null)
                {
                    _logger.LogWarning("User not found for email: {Email}", email);
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.USER) };
                    return NotFound(_response);
                }
                if (model.Role.Contains(SD.PARENT_ROLE))
                {
                    _response.Result = _mapper.Map<ApplicationUserDTO>(model);
                    _response.StatusCode = HttpStatusCode.OK;
                    return Ok(_response);
                }
                else
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.USER) };
                    _response.StatusCode = HttpStatusCode.NotFound;
                    return NotFound(_response);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving user with email: {Email}", email);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpGet("lock/{userId}")]
        public async Task<ActionResult<APIResponse>> LockoutUser(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Received a bad request with an empty or null userId.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                ApplicationUser model = await _userRepository.LockoutUser(userId);
                if (model == null)
                {
                    _logger.LogWarning("User with userId: {UserId} not found or unable to lockout.", userId);
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.USER) };
                    return NotFound(_response);
                }
                _response.Result = _mapper.Map<ApplicationUserDTO>(model);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while locking out the user with userId: {UserId}", userId);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet("unlock/{userId}")]
        public async Task<ActionResult<APIResponse>> UnLockoutUser(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Received a bad request with an empty or null userId.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                ApplicationUser model = await _userRepository.UnlockUser(userId);
                if (model == null)
                {
                    _logger.LogWarning("User with userId: {UserId} not found or unable to unlock.", userId);
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.USER) };
                    return NotFound(_response);
                }
                _response.Result = _mapper.Map<ApplicationUserDTO>(model);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while unlocking the user with userId: {UserId}", userId);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
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
                    _logger.LogWarning("Received a bad request with an empty or null userId.");
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return NotFound(_response);
                }
                List<IdentityRole> model = await _userRepository.GetRoleByUserId(userId);
                if (model == null || model.Count == 0)
                {
                    _logger.LogWarning("User with userId: {UserId} has no roles.", userId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.USER_HAVE_NO_ROLE, userId) };
                    return BadRequest(_response);
                }
                _response.Result = _mapper.Map<List<RoleDTO>>(model);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (InvalidDataException ex)
            {
                _logger.LogError(ex, "Error occurred while updating password for User ID: {UserId}", id);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.BadRequest, _response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving roles for user with userId: {UserId}", userId);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
