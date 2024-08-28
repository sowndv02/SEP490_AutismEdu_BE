using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace backend_api.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class ClaimController : ControllerBase
    {
        private readonly IClaimRepository _claimRepository;
        private readonly IUserRepository _userRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        protected int pageSize = 0;
        protected int takeValue = 0;
        public ClaimController(IClaimRepository claimRepository, IMapper mapper, IConfiguration configuration, IUserRepository userRepository)
        {
            _userRepository = userRepository;
            pageSize = configuration.GetValue<int>("APIConfig:PageSize");
            _claimRepository = claimRepository;
            _mapper = mapper;
            _response = new();
            takeValue = configuration.GetValue<int>("APIConfig:TakeValue");
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllClaimsAsync([FromQuery] string? search, string? searchType, int pageNumber = 1)
        {
            try
            {
                List<ApplicationClaim> list = await _claimRepository.GetAllAsync(null, pageSize: pageSize, pageNumber: pageNumber);
                if (!string.IsNullOrEmpty(search) && !string.IsNullOrEmpty(searchType))
                {
                    switch (searchType.ToLower())
                    {
                        case "create":
                            list = list.Where(u => u.ClaimType.ToLower().Equals("create") && u.ClaimValue.ToLower().Contains(search.ToLower())).ToList();
                            break;
                        case "update":
                            list = list.Where(u => u.ClaimType.ToLower().Equals("update") && u.ClaimValue.ToLower().Contains(search.ToLower())).ToList();
                            break;
                        case "delete":
                            list = list.Where(u => u.ClaimType.ToLower().Equals("delete") && u.ClaimValue.ToLower().Contains(search.ToLower())).ToList();
                            break;
                        case "view":
                            list = list.Where(u => u.ClaimType.ToLower().Equals("view") && u.ClaimValue.ToLower().Contains(search.ToLower())).ToList();
                            break;
                        case "assign":
                            list = list.Where(u => u.ClaimType.ToLower().Equals("assign") && u.ClaimValue.ToLower().Contains(search.ToLower())).ToList();
                            break;
                        default:
                            list = list.Where(u => u.ClaimType.ToLower().Contains(search.ToLower()) || u.ClaimType.ToLower().Contains(search.ToLower())).ToList();
                            break;
                    }
                }

                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = _claimRepository.GetTotalClaim() };
                var result = _mapper.Map<List<ClaimDTO>>(list);
                foreach (var claim in result)
                {
                    var (totalCount, users) = await _userRepository.GetUsersForClaimAsync(claim.Id, takeValue);
                    claim.TotalUser = totalCount;
                    claim.Users = _mapper.Map<List<ApplicationUserDTO>>(users);
                }
                Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(pagination));
                _response.Result = result;
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
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

        [HttpGet("type")]
        public async Task<ActionResult<APIResponse>> GetAllClaimTypesAsync()
        {
            try
            {
                List<ApplicationClaim> list = await _claimRepository.GetAllAsync(null, null, null);
                var distinctClaimTypes = list.Select(c => c.ClaimType).Distinct().ToList();
                _response.Result = distinctClaimTypes;
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

        [HttpGet("{id:int}", Name = "GetClaimById")]
        public async Task<ActionResult<APIResponse>> GetClaimByIdAsync(int id)
        {
            try
            {
                if (id < 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { $"{id} is invalid!" };
                    return BadRequest(_response);
                }

                ApplicationClaim model = await _claimRepository.GetAsync(x => x.Id == id);

                if (model == null)
                {
                    //throw new NotFoundException(nameof(GetClaimByIdAsync), id);
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { $"Claim with ID {id} not found." };
                    return NotFound(_response);
                }
                var result = _mapper.Map<ClaimDTO>(model);

                var (totalCount, users) = await _userRepository.GetUsersForClaimAsync(result.Id, takeValue);
                result.TotalUser = totalCount;
                result.Users = _mapper.Map<List<ApplicationUserDTO>>(users);
                _response.Result = result;
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPost]
        public async Task<ActionResult<APIResponse>> CreateAsync([FromBody] ClaimDTO claimDTO)
        {
            try
            {
                if (claimDTO == null) return BadRequest(claimDTO);
                ApplicationClaim model = _mapper.Map<ApplicationClaim>(claimDTO);
                model.UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _claimRepository.CreateAsync(model);
                _response.Result = _mapper.Map<ClaimDTO>(model);
                _response.StatusCode = HttpStatusCode.Created;
                return CreatedAtRoute("GetClaimById", new { model.Id }, _response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<APIResponse>> UpdateAsync(int id, [FromBody] ClaimDTO claimDTO)
        {
            try
            {
                var claim = await _claimRepository.GetAsync(x => x.Id == id, false);
                if (claimDTO == null || id != claimDTO.Id || claim == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { "Id invalid!" };
                    return BadRequest(_response);
                }
                ApplicationClaim model = _mapper.Map<ApplicationClaim>(claimDTO);
                var result = await _claimRepository.UpdateAsync(model);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                _response.Result = result;
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
        public async Task<ActionResult<APIResponse>> DeleteAsync(int id)
        {
            try
            {
                if (id == 0) return BadRequest();
                var obj = await _claimRepository.GetAsync(x => x.Id == id, false);

                if (obj == null) return NotFound();

                await _claimRepository.RemoveAsync(obj);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);

            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
