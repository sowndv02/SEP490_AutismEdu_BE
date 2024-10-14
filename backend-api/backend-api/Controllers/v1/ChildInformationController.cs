using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models.DTOs.UpdateDTOs;
using backend_api.Repository;
using backend_api.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Linq.Expressions;
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
        //[Authorize]
        public async Task<ActionResult<APIResponse>> CreateAsync(ChildInformationCreateDTO childInformationCreateDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;                
                if (childInformationCreateDTO == null || string.IsNullOrEmpty(userId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }

                var isChildExist = await _childInfoRepository.GetAsync(x => x.Name.Equals(childInformationCreateDTO.Name) && x.ParentId.Equals(userId));
                if (isChildExist != null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.CHILD_NAME_DUPLICATE };
                    return BadRequest(_response);
                }

                ChildInformation model = _mapper.Map<ChildInformation>(childInformationCreateDTO);
                model.ParentId = userId;
                model.CreatedDate = DateTime.Now;
                var childInfo = await _childInfoRepository.CreateAsync(model);

                _response.Result = _mapper.Map<ChildInformationDTO>(childInfo);
                _response.StatusCode = HttpStatusCode.Created;
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

        [HttpGet("{parentId}")]
        //[Authorize]
        public async Task<ActionResult<APIResponse>> GetParentChildInfo(string parentId)
        {
            try
            {

                
                if(string.IsNullOrEmpty(parentId))
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages = new List<string>() { SD.BAD_REQUEST_MESSAGE };
                    return StatusCode((int)HttpStatusCode.InternalServerError, _response);
                }

                var childInfos = await _childInfoRepository.GetParentChildInformationAsync(parentId);
                
                _response.Result = _mapper.Map<List<ChildInformationDTO>>(childInfos);
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

        [HttpPut]
        //[Authorize]
        public async Task<IActionResult> UpdateAsync([FromBody] ChildInformationUpdateDTO updateDTO)
        {
            try
            {
                var model = await _childInfoRepository.GetAsync(x => x.Id == updateDTO.ChildId);
                if(model == null)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages = new List<string>() { SD.NOT_FOUND_MESSAGE };
                    return StatusCode((int)HttpStatusCode.InternalServerError, _response);
                }

                var isChildExist = await _childInfoRepository.GetAsync(x => x.Name.Equals(updateDTO.Name) && !x.Name.Equals(model.Name) && x.ParentId.Equals(model.ParentId));
                if (isChildExist != null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.CHILD_NAME_DUPLICATE };
                    return BadRequest(_response);
                }

                if (!string.IsNullOrEmpty(updateDTO.Name))
                {
                    model.Name = updateDTO.Name;
                }
                if (!string.IsNullOrEmpty(updateDTO.BirthDate.ToString()))
                {
                    model.BirthDate = updateDTO.BirthDate;
                }

                model.isMale = updateDTO.isMale;     
                model.UpdatedDate = DateTime.Now;
                var childInfo = await _childInfoRepository.UpdateAsync(model);

                _response.Result = _mapper.Map<ChildInformationDTO>(childInfo);
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

    }
}
