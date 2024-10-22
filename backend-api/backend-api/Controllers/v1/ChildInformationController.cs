using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models.DTOs.UpdateDTOs;
using backend_api.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace backend_api.Controllers.v1
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
        private readonly IStudentProfileRepository _studentProfileRepository;
        private readonly IBlobStorageRepository _blobStorageRepository;
        private readonly IChildInformationMediaRepository _childInformationMediaRepository;

        public ChildInformationController(IChildInformationRepository childInfoRepository, IMapper mapper,
            IStudentProfileRepository studentProfileRepository, IBlobStorageRepository blobStorageRepository
            , IChildInformationMediaRepository childInformationMediaRepository)
        {
            _childInfoRepository = childInfoRepository;
            _response = new APIResponse();
            _mapper = mapper;
            _studentProfileRepository = studentProfileRepository;
            _blobStorageRepository = blobStorageRepository;
            _childInformationMediaRepository = childInformationMediaRepository;
        }

        [HttpPost]
        public async Task<ActionResult<APIResponse>> CreateAsync([FromForm] ChildInformationCreateDTO childInformationCreateDTO)
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

                // Handle certificate media uploads
                if (childInformationCreateDTO.Medias != null)
                {
                    for (int i = 0; i < childInformationCreateDTO.Medias.Count; i++)
                    {
                        var media = childInformationCreateDTO.Medias[i];

                        if (media != null)
                        {
                            using var mediaStream = media.OpenReadStream();
                            string mediaUrl = await _blobStorageRepository.Upload(mediaStream, string.Concat(Guid.NewGuid().ToString(), Path.GetExtension(media.FileName)));

                            ChildInformationMedia childMedia = new ChildInformationMedia
                            {
                                ChildInformationId = childInfo.Id,
                                UrlPath = mediaUrl
                            };
                            await _childInformationMediaRepository.CreateAsync(childMedia);
                        }
                    }
                }

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
        public async Task<ActionResult<APIResponse>> GetParentChildInfo(string parentId)
        {
            try
            {


                if (string.IsNullOrEmpty(parentId))
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.ErrorMessages = new List<string>() { SD.BAD_REQUEST_MESSAGE };
                    return StatusCode((int)HttpStatusCode.InternalServerError, _response);
                }

                var childInfos = await _childInfoRepository.GetAllNotPagingAsync(x => x.ParentId.Equals(parentId), "ChildInformationMedias,Parent");

                _response.Result = _mapper.Map<List<ChildInformationDTO>>(childInfos.list);
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
        public async Task<IActionResult> UpdateAsync([FromBody] ChildInformationUpdateDTO updateDTO)
        {
            try
            {
                var model = await _childInfoRepository.GetAsync(x => x.Id == updateDTO.ChildId);
                if (model == null)
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

                    var studentProfile = await _studentProfileRepository.GetAsync(x => x.ChildId == model.Id);

                    if (studentProfile != null)
                    {
                        string[] names = updateDTO.Name.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                        studentProfile.StudentCode = "";
                        foreach (var name in names)
                        {
                            studentProfile.StudentCode += name.ToUpper().ElementAt(0);
                        }
                        studentProfile.StudentCode += studentProfile.ChildId;

                        await _studentProfileRepository.UpdateAsync(studentProfile);
                    }
                }
                if (!string.IsNullOrEmpty(updateDTO.BirthDate.ToString()))
                {
                    model.BirthDate = updateDTO.BirthDate;
                }

                //TODO: update child media
                //// Handle certificate media uploads
                //if (updateDTO.Medias != null)
                //{
                //    for (int i = 0; i < updateDTO.Medias.Count; i++)
                //    {
                //        var media = updateDTO.Medias[i];

                //        if (media != null)
                //        {
                //            using var mediaStream = media.OpenReadStream();
                //            string mediaUrl = await _blobStorageRepository.Upload(mediaStream, string.Concat(Guid.NewGuid().ToString(), Path.GetExtension(media.FileName)));

                //            ChildInformationMedia childMedia = await _childInformationMediaRepository.GetAsync(x => x.ChildInformationId == model.Id);
                //            childMedia.UrlPath = mediaUrl;

                //            await _childInformationMediaRepository.UpdateAsync(childMedia);
                //        }
                //    }
                //}

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
