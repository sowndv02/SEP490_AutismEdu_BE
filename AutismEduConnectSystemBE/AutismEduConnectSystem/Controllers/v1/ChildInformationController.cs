using AutismEduConnectSystem.DTOs;
using AutismEduConnectSystem.DTOs.CreateDTOs;
using AutismEduConnectSystem.DTOs.UpdateDTOs;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Net;
using System.Security.Claims;

namespace AutismEduConnectSystem.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    public class ChildInformationController : ControllerBase
    {
        private readonly IChildInformationRepository _childInfoRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;
        private readonly ILogger<ChildInformationController> _logger;
        private readonly IStudentProfileRepository _studentProfileRepository;
        private readonly IBlobStorageRepository _blobStorageRepository;
        private readonly IResourceService _resourceService;

        public ChildInformationController(IChildInformationRepository childInfoRepository, IMapper mapper, ILogger<ChildInformationController> logger,
            IStudentProfileRepository studentProfileRepository, IBlobStorageRepository blobStorageRepository, IResourceService resourceService)
        {
            _logger = logger;
            _childInfoRepository = childInfoRepository;
            _response = new APIResponse();
            _mapper = mapper;
            _studentProfileRepository = studentProfileRepository;
            _blobStorageRepository = blobStorageRepository;
            _resourceService = resourceService;
        }

        [HttpPost]
        [Authorize(Roles = SD.PARENT_ROLE)]
        public async Task<ActionResult<APIResponse>> CreateAsync([FromForm] ChildInformationCreateDTO childInformationCreateDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                if (userRoles == null || (!userRoles.Contains(SD.PARENT_ROLE)))
                {
                   
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                if (!ModelState.IsValid)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.CHILD_INFO) };
                    return BadRequest(_response);
                }
                var isChildExist = await _childInfoRepository.GetAsync(x => x.Name.Equals(childInformationCreateDTO.Name) && x.ParentId.Equals(userId), false, null, null);
                if (isChildExist != null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.CHILD_NAME) };
                    return BadRequest(_response);
                }
                ChildInformation model = _mapper.Map<ChildInformation>(childInformationCreateDTO);
                model.ParentId = userId;
                model.CreatedDate = DateTime.Now;
                if (childInformationCreateDTO.Media != null)
                {
                    using var mediaStream = childInformationCreateDTO.Media.OpenReadStream();
                    string mediaUrl = await _blobStorageRepository.Upload(mediaStream, string.Concat(Guid.NewGuid().ToString(), Path.GetExtension(childInformationCreateDTO.Media.FileName)));
                    model.ImageUrlPath = mediaUrl;
                }
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
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet("{parentId}")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetChildInfo(string parentId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                if (string.IsNullOrEmpty(parentId))
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return StatusCode((int)HttpStatusCode.BadRequest, _response);
                }
                var childInfos = await _childInfoRepository.GetAllNotPagingAsync(x => x.ParentId.Equals(parentId), "Parent", null, null, true);
                _response.Result = _mapper.Map<List<ChildInformationDTO>>(childInfos.list);
                _response.StatusCode = HttpStatusCode.OK;
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

        [HttpPut]
        [Authorize(Roles = SD.PARENT_ROLE)]
        public async Task<ActionResult<APIResponse>> UpdateAsync([FromForm] ChildInformationUpdateDTO updateDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                if (userRoles == null || (!userRoles.Contains(SD.PARENT_ROLE)))
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }
                if (!ModelState.IsValid)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.CHILD_INFO) };
                    return BadRequest(_response);
                }
                
                var model = await _childInfoRepository.GetAsync(x => x.Id == updateDTO.ChildId && x.ParentId == userId, true, null, null);
                if (model == null)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.CHILD_INFO) };
                    return StatusCode((int)HttpStatusCode.NotFound, _response);
                }

                var isChildExist = await _childInfoRepository.GetAsync(x => x.Name.Equals(updateDTO.Name) && !x.Name.Equals(model.Name) && x.ParentId.Equals(model.ParentId), false, null, null);
                if (isChildExist != null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.CHILD_NAME) };
                    return BadRequest(_response);
                }

                if (!string.IsNullOrEmpty(updateDTO.Name))
                {
                    model.Name = updateDTO.Name;

                    var studentProfile = await _studentProfileRepository.GetAsync(x => x.ChildId == model.Id, true, null, null);

                    if (studentProfile != null)
                    {
                        string[] names = updateDTO.Name.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                        studentProfile.StudentCode = "";
                        for (int i = 0; i < (names.Length > 6 ? 6 : names.Length); i++)
                        {
                            studentProfile.StudentCode += names[i].ToUpper().ElementAt(0);
                        }
                        studentProfile.StudentCode += studentProfile.ChildId;

                        await _studentProfileRepository.UpdateAsync(studentProfile);
                    }
                }
                if (!string.IsNullOrEmpty(updateDTO.BirthDate.ToString()))
                {
                    model.BirthDate = updateDTO.BirthDate;
                }

                // Update child media
                if (updateDTO.Media != null)
                {
                    using var mediaStream = updateDTO.Media.OpenReadStream();
                    string mediaUrl = await _blobStorageRepository.Upload(mediaStream, string.Concat(Guid.NewGuid().ToString(), Path.GetExtension(updateDTO.Media.FileName)), false);

                    model.ImageUrlPath = mediaUrl;
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
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
