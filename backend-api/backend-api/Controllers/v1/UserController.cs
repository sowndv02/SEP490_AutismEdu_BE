using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models.DTOs.UpdateDTOs;
using backend_api.Repository.IRepository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
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

        [HttpDelete("role/{userId}")]
        public async Task<ActionResult<APIResponse>> RemoveRoleByUserId(string userId, UserRoleDTO userRoleDTO)
        {
            try
            {
                if (!userId.Equals(userRoleDTO.UserId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { $"Data is invalid!" };
                    return BadRequest(_response);
                }
                var result = await _userRepository.RemoveRoleByUserId(userId, userRoleDTO.UserRoleIds);
                if (!result)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.InternalServerError;
                    _response.ErrorMessages = new List<string>() { "Internal sever error!" };
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
                    _response.ErrorMessages = new List<string> { $"Data is invalid!" };
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
                                _response.ErrorMessages = new List<string> { $"Cannot add role {restrictedRole} to user with role {role}." };
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
                    _response.ErrorMessages = new List<string>() { "Internal sever error!" };
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
                    _response.ErrorMessages = new List<string> { $"Data is invalid!" };
                    return BadRequest(_response);
                }
                var result = await _userRepository.RemoveClaimByUserId(userId, userClaimDTO.UserClaimIds);
                if (!result)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.InternalServerError;
                    _response.ErrorMessages = new List<string>() { "Internal sever error!" };
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
                    _response.ErrorMessages = new List<string> { $"Data is invalid!" };
                    return BadRequest(_response);
                }
                var result = await _userRepository.AddClaimToUser(userId, userClaimDTO.UserClaimIds);
                if (!result)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.InternalServerError;
                    _response.ErrorMessages = new List<string>() { "Internal sever error!" };
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
                    _response.ErrorMessages = new List<string>() { "Email is exist!" };
                    return StatusCode((int)HttpStatusCode.InternalServerError, _response);
                }
                ApplicationUser model = _mapper.Map<ApplicationUser>(createDTO);
                model.CreatedDate = DateTime.Now;
                var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
                string filePath = @"wwwroot\UserImages\" + SD.IMAGE_DEFAULT_AVATAR_NAME;
                model.ImageLocalUrl = baseUrl + $"/{SD.URL_IMAGE_USER}/" + SD.IMAGE_DEFAULT_AVATAR_NAME;
                model.ImageUrl = SD.URL_IMAGE_DEFAULT_BLOB;
                model.ImageLocalPathUrl = filePath;

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
                    _response.ErrorMessages = new List<string>() { "Error when create user" };
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
                    _response.ErrorMessages = new List<string>() { "Id invalid!" };
                    return BadRequest(_response);
                }

                var model = await _userRepository.GetAsync(x => x.Id == id);
                var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
                if (updateDTO.Image != null)
                {
                    if (!string.IsNullOrEmpty(model.ImageLocalPathUrl))
                    {
                        var oldFilePathDirectory = Path.Combine(Directory.GetCurrentDirectory(), model.ImageLocalPathUrl);
                        FileInfo file = new FileInfo(oldFilePathDirectory);
                        if (file.Exists)
                        {
                            file.Delete();
                        }
                    }
                    string fileName = userId + Path.GetExtension(updateDTO.Image.FileName);
                    string filePath = @"wwwroot\UserImage\" + fileName;

                    var directoryLocation = Path.Combine(Directory.GetCurrentDirectory(), filePath);

                    using (var fileStream = new FileStream(directoryLocation, FileMode.Create))
                    {
                        updateDTO.Image.CopyTo(fileStream);
                    }
                    model.ImageLocalPathUrl = filePath;
                    model.ImageLocalUrl = baseUrl + $"/{SD.URL_IMAGE_USER}/" + SD.IMAGE_DEFAULT_AVATAR_NAME;
                    using var stream = updateDTO.Image.OpenReadStream();
                    model.ImageUrl = await _blobStorageRepository.Upload(stream, fileName);
                }
                if (!string.IsNullOrEmpty(model.Address))
                    model.Address = updateDTO.Address;
                if (!string.IsNullOrEmpty(model.PhoneNumber))
                    model.PhoneNumber = updateDTO.PhoneNumber;
                if (!string.IsNullOrEmpty(model.FullName))
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
                Expression<Func<ApplicationUser, bool>> filter = null;
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                // TODO: HANDLE FOR ROLE
                if (userRoles.Contains(SD.STAFF_ROLE))
                {
                    filter = u => !u.Role.Contains(SD.ADMIN_ROLE);
                }
                if (!string.IsNullOrEmpty(searchType))
                {
                    switch (searchType.ToLower().Trim())
                    {
                        case "all":
                            if (!string.IsNullOrEmpty(searchValue))
                            {
                                var (totalResult, resultObject) = await _userRepository.GetAllAsync(u => (u.Email.ToLower().Contains(searchValue.ToLower())) || (!string.IsNullOrEmpty(u.FullName) && u.FullName.ToLower().Contains(searchValue.ToLower())), pageSize: pageSize, pageNumber: pageNumber);
                                totalCount = totalResult;
                                list = resultObject;
                            }
                            else
                            {
                                var (totalResult, resultObject) = await _userRepository.GetAllAsync(null, pageSize: pageSize, pageNumber: pageNumber);
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
                            var (total, result) = await _userRepository.GetAllAsync(null, pageSize: pageSize, pageNumber: pageNumber);
                            list = result;
                            totalCount = _userRepository.GetTotalUser();
                            break;

                    }
                }
                else
                {
                    var (total, result) = await _userRepository.GetAllAsync(filter, pageSize: pageSize, pageNumber: pageNumber);
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
                    _response.ErrorMessages = new List<string> { $"{id} is invalid!" };
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

        [HttpGet("lock/{userId}")]
        public async Task<IActionResult> LockoutUser(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { $"{userId} is invalid!" };
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
                    _response.ErrorMessages = new List<string> { $"{userId} is invalid!" };
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
                    _response.ErrorMessages = new List<string> { $"{userId} is null or empty!" };
                    return NotFound(_response);
                }
                List<IdentityRole> model = await _userRepository.GetRoleByUserId(userId);
                if (model == null || model.Count == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { $"{userId} is not in role!" };
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
