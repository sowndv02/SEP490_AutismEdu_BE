﻿using AutoMapper;
using backend_api.Data;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net;

namespace backend_api.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersionNeutral]
    public class RoleController : ControllerBase
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRepository _userRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        protected int takeValue = 0;
        public RoleController(IRoleRepository roleRepository, IMapper mapper, IUserRepository userRepository, IConfiguration configuration)
        {
            takeValue = configuration.GetValue<int>("APIConfig:TakeValue");
            _response = new();
            _mapper = mapper;
            _roleRepository = roleRepository;
            _userRepository = userRepository;
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
                    _response.ErrorMessages = new List<string> { $"{id} is null or empty!" };
                    return BadRequest(_response);
                }
                IdentityRole model = await _roleRepository.GetByIdAsync(id);
                if (model == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { $"{id} is not found!" };
                    return NotFound(_response);
                }
                var result = _mapper.Map<RoleDTO>(model);
                
                var (totalCount, users) = await _userRepository.GetUsersInRole(model.Name, takeValue);
                result.TotalUsersInRole = totalCount;
                result.Users = _mapper.Map<List<ApplicationUserDTO>>(users);
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

        [HttpGet("user/{userId}", Name = "GetRoleByUserId")]
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
                IdentityRole model = await _roleRepository.GetRoleByUserId(userId);
                if(model == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { $"{userId} is not in role!" };
                    return BadRequest(_response);
                }
                _response.Result = _mapper.Map<RoleDTO>(model);
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
        public async Task<ActionResult<APIResponse>> CreateRoleAsync(RoleDTO roleDTO)
        {
            try
            {
                if (await _roleRepository.GetByNameAsync(roleDTO.Name) != null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { $"{roleDTO.Name} existed!" };
                    return BadRequest(_response);
                }
                if (roleDTO == null) return BadRequest(roleDTO);
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
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.ErrorMessages = new List<string>() { $"Remove role successful!" };
                        return Ok(_response);
                    }
                    else
                    {
                        _response.IsSuccess = false;
                        _response.StatusCode = HttpStatusCode.NotFound;
                        _response.ErrorMessages = new List<string>() { $"Cannot delete this role, since there are users assigned to this role with role id is: {roleId}" };
                        return Ok(_response);
                    }
                }
                else
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.ErrorMessages = new List<string>() { $"Not found with role id is: {roleId}" };
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