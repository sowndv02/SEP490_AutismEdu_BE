using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Repository.IRepository;
using backend_api.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace backend_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiVersionNeutral]
    public class PackagePaymentController : ControllerBase
    {
        private readonly IPackagePaymentRepository _packagePaymentRepository;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        private readonly ILogger<PackagePaymentController> _logger;
        private readonly IResourceService _resourceService;
        public PackagePaymentController(IPackagePaymentRepository packagePaymentRepository,
            IMapper mapper, IResourceService resourceService,
            ILogger<PackagePaymentController> logger)
        {
            _response = new APIResponse();
            _mapper = mapper;
            _packagePaymentRepository = packagePaymentRepository;
            _resourceService = resourceService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Roles = SD.MANAGER_ROLE)]
        public async Task<ActionResult<APIResponse>> CreateAsync(PackagePaymentCreateDTO createDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model state is invalid. Returning BadRequest.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.PACKET_PAYMENT) };
                    return BadRequest(_response);
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var newModel = _mapper.Map<PackagePayment>(createDTO);

                newModel.SubmitterId = userId;
                newModel.IsActive = false;
                newModel.VersionNumber = await _packagePaymentRepository.GetNextVersionNumberAsync(createDTO.OriginalId);
                if (createDTO.OriginalId == 0)
                {
                    newModel.OriginalId = null;
                }
                await _packagePaymentRepository.CreateAsync(newModel);
                _response.StatusCode = HttpStatusCode.Created;
                _response.Result = _mapper.Map<PackagePaymentDTO>(newModel);
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while creating payment history: {ex.Message}");
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllAsync()
        {
            try
            {
                var list = await _packagePaymentRepository.GetAllNotPagingAsync(null, "Submitter", null);
                _response.IsSuccess = true;
                _response.Result = _mapper.Map<List<PackagePaymentDTO>>(list);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _logger.LogError("Error occurred while creating an assessment question: {Message}", ex.Message);
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpDelete("{id:int}")]
        [Authorize(Roles = SD.MANAGER_ROLE)]
        public async Task<ActionResult<APIResponse>> DeleteAsync(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            try
            {
                if (id == 0)
                {
                    _logger.LogWarning("Invalid curriculum ID: {CurriculumId}. Returning BadRequest.", id);
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                var model = await _packagePaymentRepository.GetAsync(x => x.Id == id, false, null);

                if (model == null)
                {
                    _logger.LogWarning("Packet payment not found for ID: {id} and User ID: {userId}. Returning BadRequest.", id, userId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.CERTIFICATE) };
                    return BadRequest(_response);
                }
                model.IsDeleted = true;
                await _packagePaymentRepository.UpdateAsync(model);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting Packet payment ID: {id}", id);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

    }
}
