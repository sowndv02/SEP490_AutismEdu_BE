using AutoMapper;
using backend_api.Models;
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
                    var objMedia = new CertificateMedia() { CertificateId = certificate.Id, UrlPath = url };
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
    }
}
