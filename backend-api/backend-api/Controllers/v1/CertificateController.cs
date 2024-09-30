using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models.DTOs.UpdateDTOs;
using backend_api.Repository.IRepository;
using backend_api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;

namespace backend_api.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class CertificateController : ControllerBase
    {

        private readonly IUserRepository _userRepository;
        private readonly ICertificateRepository _certificateRepository;
        private readonly ICertificateMediaRepository _certificateMediaRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IBlobStorageRepository _blobStorageRepository;
        private readonly ILogger<CertificateController> _logger;
        private readonly IMapper _mapper;
        private readonly FormatString _formatString;
        protected APIResponse _response;
        protected int pageSize = 0;
        public CertificateController(IUserRepository userRepository, ICertificateRepository certificateRepository,
            ILogger<CertificateController> logger, IBlobStorageRepository blobStorageRepository,
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
            _certificateRepository = certificateRepository;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<APIResponse>> CreateAsync(CertificateCreateDTO certificateCreateDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (certificateCreateDTO == null || string.IsNullOrEmpty(userId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { $"{userId} not exist!" };
                    return BadRequest(_response);
                }
                Certificate model = _mapper.Map<Certificate>(certificateCreateDTO);
                model.SubmiterId = userId;
                model.CreatedDate = DateTime.Now;
                var certificate = await _certificateRepository.CreateAsync(model);
                foreach (var media in certificateCreateDTO.Medias)
                {
                    using var stream = media.OpenReadStream();
                    var url = await _blobStorageRepository.Upload(stream, userId + Path.GetExtension(media.FileName));
                    var objMedia = new CertificateMedia() { CertificateId = certificate.Id, UrlPath = url, CreatedDate = DateTime.Now };
                    await _certificateMediaRepository.CreateAsync(objMedia);
                }
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

        [HttpPut("changeStatus/{id}")]
        //[Authorize(Policy = "UpdateCertificatePolicy")]
        public async Task<IActionResult> ApproveOrRejectTutor(ChangeStatusCertificateDTO changeStatusCertificateDTO)
        {
            try
            {
                if (changeStatusCertificateDTO.Id <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { $"{changeStatusCertificateDTO.Id} is invalid!" };
                    return BadRequest(_response);
                }
                Certificate model = await _certificateRepository.GetAsync(x => x.Id == changeStatusCertificateDTO.Id, false, null, null);
                model.IsApprove = !model.IsApprove;

                if (!string.IsNullOrEmpty(changeStatusCertificateDTO.Feedback))
                    model.Feedback = changeStatusCertificateDTO.Feedback;
                model.UpdatedDate = DateTime.Now;
                await _certificateRepository.UpdateAsync(model);
                _response.Result = _mapper.Map<TutorDTO>(model);
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

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? status = "all", int pageNumber = 1)
        {
            try
            {
                int totalCount = 0;
                List<Certificate> list = new();
                if (!string.IsNullOrEmpty(status)) status = "all";
                Expression<Func<Certificate, bool>> filter = null;
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                if (userRoles.Contains(SD.USER_ROLE))
                {
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    filter = cert => cert.SubmiterId == userId;
                }
                switch (status)
                {
                    case "all":
                        var (count, result) = await _certificateRepository.GetAllAsync(filter, "CertificateMedias", pageSize: pageSize, pageNumber: pageNumber, x => x.CreatedDate, true);
                        list = result;
                        totalCount = count;
                        break;
                    case "approve":
                        var (countResult, resultObj) = await _certificateRepository.GetAllAsync(x => x.IsApprove == true && (filter == null || filter.Compile()(x)), "CertificateMedias", pageSize: pageSize, pageNumber: pageNumber, x => x.CreatedDate, true);
                        list = resultObj;
                        totalCount = countResult;
                        break;
                    case "reject":
                        var (countObj, results) = await _certificateRepository.GetAllAsync(x => x.IsApprove == false && (filter == null || filter.Compile()(x)), "CertificateMedias", pageSize: pageSize, pageNumber: pageNumber, x => x.CreatedDate, true);
                        list = results;
                        totalCount = countObj;
                        break;
                    default:
                        var (defaultCount, defaultResult) = await _certificateRepository.GetAllAsync(filter, "CertificateMedias", pageSize: pageSize, pageNumber: pageNumber, x => x.CreatedDate, true);
                        list = defaultResult;
                        totalCount = defaultCount;
                        break;
                }
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = totalCount };
                _response.Result = _mapper.Map<List<CertificateDTO>>(list);
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

        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse>> GetById(int id)
        {
            try
            {
                var result = await _certificateRepository.GetAsync(x => x.Id == id, false, "CertificateMedias", null);
                _response.Result = _mapper.Map<CertificateDTO>(result);
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

        [HttpPut("changeStatus/{id}")]
        //[Authorize(Policy = "UpdateCertificatePolicy")]
        public async Task<IActionResult> ApproveOrRejectTutor(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { $"{id} is invalid!" };
                    return BadRequest(_response);
                }
                Certificate model = await _certificateRepository.GetAsync(x => x.Id == id, false, "CertificateMedias", null);
                model.IsApprove = !model.IsApprove;
                model.UpdatedDate = DateTime.Now;
                await _certificateRepository.UpdateAsync(model);
                _response.Result = _mapper.Map<CertificateDTO>(model);
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
