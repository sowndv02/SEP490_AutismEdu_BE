using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace AutismEduConnectSystem.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersionNeutral]
    public class AzureBlobController : ControllerBase
    {
        private IBlobStorageRepository _blobStorageRepository;
        private ILogger<AzureBlobController> _logger;
        private IResourceService _resourceService;
        protected APIResponse _response;
        public AzureBlobController(IBlobStorageRepository blobStorageRepository, 
            IResourceService resourceService, ILogger<AzureBlobController> logger)
        {
            _blobStorageRepository = blobStorageRepository;
            _resourceService = resourceService;
            _response = new();
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<APIResponse>> UploadImg(IFormFile file, bool isPrivate = false)
        {
            try
            {
                using var stream = file.OpenReadStream();
                var fileName = file.FileName;
                var url = await _blobStorageRepository.Upload(stream, fileName, isPrivate);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = url;
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
        public IActionResult GetSaSUrl(string blobUrl)
        {
            try
            {
                string url = _blobStorageRepository.GetBlobSasUrl(blobUrl);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Result = url;
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
