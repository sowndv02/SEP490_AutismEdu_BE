using backend_api.Models;
using backend_api.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace backend_api.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersionNeutral]
    public class AzureBlobController : ControllerBase
    {
        private IBlobStorageRepository _blobStorageRepository;
        protected APIResponse _response;
        public AzureBlobController(IBlobStorageRepository blobStorageRepository)
        {
            _blobStorageRepository = blobStorageRepository;
            _response = new();
        }

        [HttpPost]
        public async Task<IActionResult> UploadImg(IFormFile file, bool isPrivate = false)
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
                _response.ErrorMessages = new List<string>() { ex.Message };
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
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
