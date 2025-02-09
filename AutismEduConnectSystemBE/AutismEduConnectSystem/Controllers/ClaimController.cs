﻿using AutismEduConnectSystem.DTOs;
using AutismEduConnectSystem.DTOs.CreateDTOs;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.Utils;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;

namespace AutismEduConnectSystem.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersionNeutral]
    public class ClaimController : ControllerBase
    {
        private readonly IClaimRepository _claimRepository;
        private readonly IUserRepository _userRepository;
        private readonly FormatString _formatString;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        protected int pageSize = 0;
        protected int takeValue = 0;
        private readonly IResourceService _resourceService;

        public ClaimController(IClaimRepository claimRepository, IMapper mapper, IConfiguration configuration,
            IUserRepository userRepository, FormatString formatString, IResourceService resourceService)
        {
            _userRepository = userRepository;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _claimRepository = claimRepository;
            _mapper = mapper;
            _response = new();
            takeValue = int.Parse(configuration["APIConfig:TakeValue"]);
            _formatString = formatString;
            _resourceService = resourceService;
        }

        [HttpPut("reset/{id}")]
        public async Task<ActionResult<APIResponse>> ResetAsync(int? id)
        {
            try
            {
                var claim = await _claimRepository.GetAsync(x => x.Id == id, false);
                if (claim == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.CLAIM) };
                    return NotFound(_response);
                }
                ApplicationClaim model = _mapper.Map<ApplicationClaim>(claim);
                model.ClaimValue = model.DefaultClaimValue;
                model.ClaimType = model.DefaultClaimType;
                model.UpdatedDate = DateTime.Now;
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
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }



        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllClaimsAsync([FromQuery] string? searchValue, string? searchType, int pageNumber = 1, string? userId = null)
        {
            try
            {
                Expression<Func<ApplicationClaim, bool>>? filter = u => true;
                Expression<Func<ApplicationClaim, bool>> defaultFilter = u => true;

                if (!string.IsNullOrEmpty(searchType))
                {
                    switch (searchType.ToLower().Trim())
                    {
                        case "create":
                            filter = u => u.ClaimType.ToLower().Equals("create");
                            break;
                        case "update":
                            filter = u => u.ClaimType.ToLower().Equals("update");
                            break;
                        case "delete":
                            filter = u => u.ClaimType.ToLower().Equals("delete");
                            break;
                        case "view":
                            filter = u => u.ClaimType.ToLower().Equals("view");
                            break;
                        case "assign":
                            filter = u => u.ClaimType.ToLower().Equals("assign");
                            break;
                    }
                }
                int totalClaim = 0;
                if (!string.IsNullOrEmpty(searchValue))
                {
                    Expression<Func<ApplicationClaim, bool>> searchFilter = u => u.ClaimValue.ToLower().Contains(searchValue.ToLower());

                    var combinedFilter = Expression.Lambda<Func<ApplicationClaim, bool>>(
                        Expression.AndAlso(filter.Body, Expression.Invoke(searchFilter, filter.Parameters)),
                        filter.Parameters
                    );

                    filter = combinedFilter;
                }


                List<UserClaim> userClaims = null;
                if (!string.IsNullOrEmpty(userId))
                {
                    userClaims = await _userRepository.GetClaimByUserIdAsync(userId);
                }
                var (total, list) = await _claimRepository.GetAllAsync(filter, pageSize: pageSize, pageNumber: pageNumber, userClaims: userClaims);
                List<ApplicationClaim> claims = list;
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = total };
                var result = _mapper.Map<List<ClaimDTO>>(list);
                foreach (var claim in result)
                {
                    var (totalCount, users) = await _userRepository.GetUsersForClaimAsync(claim.Id, takeValue);
                    claim.TotalUser = totalCount;
                    claim.Users = _mapper.Map<List<ApplicationUserDTO>>(users);
                }
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
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet("type")]
        public async Task<ActionResult<APIResponse>> GetAllClaimTypesAsync()
        {
            try
            {
                var (total, list) = await _claimRepository.GetAllNotPagingAsync(null, null, null);
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
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
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
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.CLAIM) };
                    return BadRequest(_response);
                }

                ApplicationClaim model = await _claimRepository.GetAsync(x => x.Id == id);

                if (model == null)
                {
                    //throw new NotFoundException(nameof(GetClaimByIdAsync), id);
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.CLAIM) };
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
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPost]
        public async Task<ActionResult<APIResponse>> CreateAsync([FromBody] ClaimCreateDTO claimDTO)
        {
            try
            {
                if (claimDTO == null) return BadRequest(claimDTO);
                ApplicationClaim model = _mapper.Map<ApplicationClaim>(claimDTO);
                model.UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(model.UserId))
                {
                    //
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                var claimExist = await _claimRepository.GetAsync(x => x.ClaimType == claimDTO.ClaimType && x.ClaimValue == claimDTO.ClaimValue, false);
                if (claimExist != null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.CLAIM) };
                    return BadRequest(_response);
                }
                model.ClaimValue = _formatString.FormatStringUpperCaseFirstChar(model.ClaimValue);
                model.ClaimType = _formatString.FormatStringUpperCaseFirstChar(model.ClaimType);
                model.DefaultClaimValue = _formatString.FormatStringUpperCaseFirstChar(model.ClaimValue);
                model.DefaultClaimType = _formatString.FormatStringUpperCaseFirstChar(model.ClaimType);
                model.CreatedDate = DateTime.Now;
                await _claimRepository.CreateAsync(model);
                _response.Result = _mapper.Map<ClaimDTO>(model);
                _response.StatusCode = HttpStatusCode.Created;
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
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.CLAIM) };
                    return BadRequest(_response);
                }
                var claimExist = await _claimRepository.GetAsync(x => x.ClaimType == claimDTO.ClaimType && x.ClaimValue == claimDTO.ClaimValue, false);
                if (claimExist != null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.CLAIM) };
                    return BadRequest(_response);
                }
                ApplicationClaim model = _mapper.Map<ApplicationClaim>(claimDTO);
                model.ClaimValue = _formatString.FormatStringUpperCaseFirstChar(model.ClaimValue);
                model.ClaimType = _formatString.FormatStringUpperCaseFirstChar(model.ClaimType);
                model.UpdatedDate = DateTime.Now;
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
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
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
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
