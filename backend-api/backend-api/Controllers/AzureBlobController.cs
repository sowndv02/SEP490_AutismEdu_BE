using backend_api.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend_api.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersionNeutral]
    public class AzureBlobController : ControllerBase
    {
        private IBlobStorageRepository _blobStorageRepository;
        public AzureBlobController(IBlobStorageRepository blobStorageRepository)
        {
            _blobStorageRepository = blobStorageRepository;
        }

        [HttpPost]
        public async Task<IActionResult> UploadImg(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            var fileName = file.FileName;
            var url = await _blobStorageRepository.UploadImg(stream, fileName); 
            return Ok(url);
        }
    }
}
