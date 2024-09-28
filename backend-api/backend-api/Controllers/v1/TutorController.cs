using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Repository.IRepository;
using backend_api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace backend_api.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class TutorController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ITutorRepository _tutorRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IBlobStorageRepository _blobStorageRepository;
        private readonly ILogger<TutorController> _logger;
        private readonly IMapper _mapper;
        private readonly FormatString _formatString;
        protected APIResponse _response;
        protected int pageSize = 0;
        public TutorController(IUserRepository userRepository, ITutorRepository tutorRepository,
            ILogger<TutorController> logger, IBlobStorageRepository blobStorageRepository,
            IMapper mapper, IConfiguration configuration, IRoleRepository roleRepository, FormatString formatString)
        {
            _formatString = formatString;
            _roleRepository = roleRepository;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _response = new APIResponse();
            _mapper = mapper;
            _blobStorageRepository = blobStorageRepository;
            _logger = logger;
            _userRepository = userRepository;
            _tutorRepository = tutorRepository;
        }


        [HttpPost]
        [Authorize]
        public async Task<ActionResult<APIResponse>> RegisterTutorAsync(TutorCreateDTO tutorCreateDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (tutorCreateDTO == null || string.IsNullOrEmpty(userId) || _userRepository.GetAsync(x => x.Id.Equals(userId), true, "TutorProfile").GetAwaiter().GetResult()?.TutorProfile != null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { $"{userId} not exist!" };
                    return BadRequest(_response);
                }
                tutorCreateDTO.FormalName = _formatString.FormatStringFormalName(tutorCreateDTO.FormalName);
                Tutor model = _mapper.Map<Tutor>(tutorCreateDTO);
                model.CreatedDate = DateTime.Now;
                await _tutorRepository.CreateAsync(model);
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

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? seạch, int pageNumber = 1)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int totalCount = 0;
                List<ApplicationUser> list = new();

                if (!string.IsNullOrEmpty(seạch))
                {
                    var (count, result) = await _userRepository.GetAllAsync(u => (u.Email.ToLower().Contains(seạch.ToLower())) || (!string.IsNullOrEmpty(u.TutorProfile.FormalName) && u.TutorProfile.FormalName.ToLower().Contains(seạch.ToLower())), "TutorProfile", pageSize: pageSize, pageNumber: pageNumber);
                    list = result;
                    totalCount = count;
                }
                else
                {
                    var (count, result) = await _userRepository.GetAllAsync(null, "TutorProfile",pageSize: pageSize, pageNumber: pageNumber);
                    list = result;
                    totalCount = count;
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

    }
}
