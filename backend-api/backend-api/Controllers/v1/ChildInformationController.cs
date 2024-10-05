using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Repository;
using backend_api.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace backend_api.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class ChildInformationController : ControllerBase
    {
        private readonly IChildInformationRepository _childInfoRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;

        public ChildInformationController(IChildInformationRepository childInfoRepository, IMapper mapper)
        {
            _childInfoRepository = childInfoRepository;
            _response = new APIResponse();
            _mapper = mapper;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<APIResponse>> CreateAsync(ChildInformationCreateDTO childInformationCreateDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;                
                if (childInformationCreateDTO == null || string.IsNullOrEmpty(userId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { $"{userId} not exist!" };
                    return BadRequest(_response);
                }

                ChildInformation model = _mapper.Map<ChildInformation>(childInformationCreateDTO);
                model.ParentId = userId;
                model.CreatedDate = DateTime.Now;
                var childInfo = await _childInfoRepository.CreateAsync(model);

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

        [HttpGet("getByEmail/{email}")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetChildInfoByParentEmail(string email)
        {
            try
            {
                var childInfos = await _childInfoRepository.GetChildByParentEmailAsync(email);

                if (childInfos == null)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages = new List<string>() { "Email doesn't exist" };
                    return StatusCode((int)HttpStatusCode.InternalServerError, _response);
                }
                List<ChildInformationDTO> result = new List<ChildInformationDTO>();
                foreach(var childInfo in childInfos)
                {
                    result.Add(_mapper.Map<ChildInformationDTO>(childInfo));
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
    }
}
