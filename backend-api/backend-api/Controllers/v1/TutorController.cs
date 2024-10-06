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
    //[Authorize]
    public class TutorController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ITutorRepository _tutorRepository;
        private readonly ITutorRegistrationRequestRepository _tutorRegistrationRequestRepository;
        private readonly ICurriculumRepository _curriculumRepository;
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
        public TutorController(IUserRepository userRepository, ITutorRepository tutorRepository,
            ILogger<TutorController> logger, IBlobStorageRepository blobStorageRepository,
            IMapper mapper, IConfiguration configuration, IRoleRepository roleRepository,
            FormatString formatString, IWorkExperienceRepository workExperienceRepository,
            ICertificateRepository certificateRepository, ICertificateMediaRepository certificateMediaRepository, 
            ITutorRegistrationRequestRepository tutorRegistrationRequestRepository, ICurriculumRepository curriculumRepository)
        {
            _curriculumRepository = curriculumRepository;
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


        //[HttpGet]
        //public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? seạch, int pageNumber = 1)
        //{
        //    try
        //    {
        //        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //        int totalCount = 0;
        //        List<Tutor> list = new();

        //        //if (!string.IsNullOrEmpty(seạch))
        //        //{
        //        //    var (count, result) = await _tutorRepository.GetAllAsync(u => (!string.IsNullOrEmpty(u.FormalName) && u.FormalName.ToLower().Contains(seạch.ToLower())), "User", pageSize: pageSize, pageNumber: pageNumber, x => x.CreatedDate, true);
        //        //    list = result;
        //        //    totalCount = count;
        //        //}
        //        //else
        //        //{
        //        //    var (count, result) = await _tutorRepository.GetAllAsync(null, "User", pageSize: pageSize, pageNumber: pageNumber, x => x.CreatedDate, true);
        //        //    list = result;
        //        //    totalCount = count;
        //        //}
        //        //Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = totalCount };
        //        //_response.Result = _mapper.Map<List<TutorDTO>>(list);
        //        //_response.StatusCode = HttpStatusCode.OK;
        //        //_response.Pagination = pagination;
        //        //return Ok(_response);
        //    }
        //    catch (Exception ex)
        //    {
        //        _response.IsSuccess = false;
        //        _response.StatusCode = HttpStatusCode.InternalServerError;
        //        _response.ErrorMessages = new List<string>() { ex.Message };
        //        return StatusCode((int)HttpStatusCode.InternalServerError, _response);
        //    }
        //}

        //[HttpGet("{id}")]
        //public async Task<ActionResult<APIResponse>> GetById(string id)
        //{
        //    try
        //    {
        //        var result = await _tutorRepository.GetAsync(x => x.UserId == id, false, "User", null);
        //        _response.Result = _mapper.Map<TutorDTO>(result);
        //        _response.StatusCode = HttpStatusCode.OK;
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

        //[HttpPut("changeStatus/{userId}")]
        //[Authorize(Policy = "UpdateTutorPolicy")]
        //public async Task<IActionResult> ApproveOrRejectTutor(string userId)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(userId))
        //        {
        //            _response.StatusCode = HttpStatusCode.BadRequest;
        //            _response.IsSuccess = false;
        //            _response.ErrorMessages = new List<string> { $"{userId} is invalid!" };
        //            return BadRequest(_response);
        //        }
        //        Tutor model = await _tutorRepository.GetAsync(x => x.UserId == userId, false, "User", null);
        //        model.IsApprove = !model.IsApprove;
        //        model.UpdatedDate = DateTime.Now;
        //        await _tutorRepository.UpdateAsync(model);

        //        // Update reject certificate
        //        if (!model.IsApprove)
        //        {
        //            Certificate certificates = await _certificateRepository.GetAsync(x => x.SubmiterId == userId, false, "CertificateMedias", null);
        //            model.IsApprove = false;
        //            model.UpdatedDate = DateTime.Now;
        //            await _certificateRepository.UpdateAsync(certificates);
        //        }
        //        _response.Result = _mapper.Map<TutorDTO>(model);
        //        _response.StatusCode = HttpStatusCode.OK;
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
