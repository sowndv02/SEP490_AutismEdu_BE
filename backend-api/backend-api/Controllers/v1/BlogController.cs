using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Repository.IRepository;
using backend_api.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace backend_api.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class BlogController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ITutorRepository _tutorRepository;
        private readonly ITutorRegistrationRequestRepository _tutorRegistrationRequestRepository;
        private readonly IBlogRepository _blogRepository;
        private readonly IWorkExperienceRepository _workExperienceRepository;
        private readonly ICertificateMediaRepository _certificateMediaRepository;
        private readonly ICertificateRepository _certificateRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IBlobStorageRepository _blobStorageRepository;
        private readonly ILogger<TutorController> _logger;
        private readonly IMapper _mapper;
        private readonly FormatString _formatString;
        protected APIResponse _response;
        protected int pageSize = 0;

        public BlogController(IUserRepository userRepository, ITutorRepository tutorRepository,
            ILogger<TutorController> logger, IBlobStorageRepository blobStorageRepository,
            IMapper mapper, IConfiguration configuration, IRoleRepository roleRepository,
            FormatString formatString, IWorkExperienceRepository workExperienceRepository,
            ICertificateRepository certificateRepository, ICertificateMediaRepository certificateMediaRepository,
            ITutorRegistrationRequestRepository tutorRegistrationRequestRepository, IBlogRepository blogRepository)
        {
            _blogRepository = blogRepository;
            _formatString = formatString;
            _roleRepository = roleRepository;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _response = new APIResponse();
            _mapper = mapper;
            _blobStorageRepository = blobStorageRepository;
            _logger = logger;
            _userRepository = userRepository;
            _tutorRepository = tutorRepository;
            _workExperienceRepository = workExperienceRepository;
            _certificateRepository = certificateRepository;
            _certificateMediaRepository = certificateMediaRepository;
            _tutorRegistrationRequestRepository = tutorRegistrationRequestRepository;
        }


        [HttpPost]
        //[Authorize]
        public async Task<ActionResult<APIResponse>> CreateAsync([FromForm] BlogCreateDTO createDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (createDTO == null || string.IsNullOrEmpty(userId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }
                Blog model = _mapper.Map<Blog>(createDTO);
                model.AuthorId = userId;
                model.CreatedDate = DateTime.Now;
                await _blogRepository.CreateAsync(model);
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

        //[HttpPut("changeStatus/{id}")]
        ////[Authorize(Policy = "UpdateTutorPolicy")]
        //public async Task<IActionResult> ApproveOrRejectBlogRequest(ChangeStatusDTO changeStatusDTO)
        //{
        //    try
        //    {
        //        var userId = _userRepository.GetAsync(x => x.Email == SD.ADMIN_EMAIL_DEFAULT).GetAwaiter().GetResult().Id;
        //        //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //        //if (string.IsNullOrEmpty(userId))
        //        //{
        //        //    _response.StatusCode = HttpStatusCode.BadRequest;
        //        //    _response.IsSuccess = false;
        //        //    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
        //        //    return BadRequest(_response);
        //        //}

        //        Curriculum model = await _curriculumRepository.GetAsync(x => x.Id == changeStatusDTO.Id, false, null, null);
        //        if (changeStatusDTO.StatusChange == (int)Status.APPROVE)
        //        {
        //            model.RequestStatus = Status.APPROVE;
        //            model.UpdatedDate = DateTime.Now;
        //            model.ApprovedId = userId;
        //            await _curriculumRepository.UpdateAsync(model);
        //            _response.Result = _mapper.Map<CurriculumDTO>(model);
        //            _response.StatusCode = HttpStatusCode.OK;
        //            _response.IsSuccess = true;
        //            return Ok(_response);
        //        }
        //        else if (changeStatusDTO.StatusChange == (int)Status.REJECT)
        //        {
        //            // Handle for reject
        //            model.RejectionReason = changeStatusDTO.RejectionReason;
        //            model.UpdatedDate = DateTime.Now;
        //            model.ApprovedId = userId;
        //            await _curriculumRepository.UpdateAsync(model);
        //            _response.Result = _mapper.Map<CurriculumDTO>(model);
        //            _response.StatusCode = HttpStatusCode.OK;
        //            _response.IsSuccess = true;
        //            return Ok(_response);
        //        }

        //        _response.StatusCode = HttpStatusCode.NoContent;
        //        _response.StatusCode = HttpStatusCode.NoContent;
        //        _response.IsSuccess = true;
        //        return Ok(_response);
        //    }
        //    catch (Exception ex)
        //    {
        //        _response.IsSuccess = false;
        //        _response.StatusCode = HttpStatusCode.InternalServerError;
        //        _response.ErrorMessages = new List<string>() { ex.Message };
        //        return StatusCode((int)HttpStatusCode.InternalServerError, _response);
        //    }
        //}
    }
}
