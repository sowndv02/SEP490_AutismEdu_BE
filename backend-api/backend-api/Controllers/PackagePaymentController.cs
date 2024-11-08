using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models.DTOs.UpdateDTOs;
using backend_api.Repository;
using backend_api.Repository.IRepository;
using backend_api.Services.IServices;
using backend_api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using static backend_api.SD;

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
                newModel.IsActive = createDTO.IsActive;
                newModel.VersionNumber = await _packagePaymentRepository.GetNextVersionNumberAsync(createDTO.OriginalId);
                await _packagePaymentRepository.DeactivatePreviousVersionsAsync(createDTO.OriginalId);
                if (createDTO.OriginalId == 0)
                {
                    newModel.OriginalId = null;
                }
                var result = await _packagePaymentRepository.CreateAsync(newModel);
                _response.StatusCode = HttpStatusCode.Created;
                _response.Result = _mapper.Map<PackagePaymentDTO>(result);
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
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? status = SD.STATUS_ALL)
        {
            try
            {

                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

                Expression<Func<PackagePayment, bool>> filter = u => true;
                if (!string.IsNullOrEmpty(status) && status != SD.STATUS_ALL)
                {
                    switch (status.ToLower())
                    {
                        case "hide":
                            filter = filter.AndAlso(x => !x.IsActive);
                            break;
                        case "show":
                            filter = filter.AndAlso(x => !x.IsActive);
                            break;
                    }
                }
                var result = new List<PackagePayment>();
                if (userRoles != null && userRoles.Contains(SD.MANAGER_ROLE))
                {
                    var(count, list) = await _packagePaymentRepository.GetAllNotPagingAsync(filter, "Submitter", null, x => x.Price, true);
                    result = list;
                }
                else
                {
                    var (count, list) = await _packagePaymentRepository.GetAllNotPagingAsync(x => x.IsActive, "Submitter", null, x => x.Price, true);
                    result = list;
                }
                _response.IsSuccess = true;
                _response.Result = _mapper.Map<List<PackagePaymentDTO>>(result);
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


        [HttpPut("changeStatus/{id}")]
        [Authorize(Roles = SD.MANAGER_ROLE)]
        public async Task<IActionResult> UpdateStatus(UpdateActiveDTO updateActiveDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model state is invalid. Returning BadRequest.");

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.WORK_EXPERIENCE) };
                    return BadRequest(_response);
                }

                PackagePayment model = await _packagePaymentRepository.GetAsync(x => x.Id == updateActiveDTO.Id, true, null, null);
                if (model == null)
                {
                    _logger.LogWarning("Package payment with ID: {Id} is either not found.", updateActiveDTO.Id);

                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.WORK_EXPERIENCE) };
                    return BadRequest(_response);
                }
                model.IsActive = updateActiveDTO.IsActive;
                model.UpdatedDate = DateTime.Now;
                await _packagePaymentRepository.UpdateAsync(model);
                _response.Result = _mapper.Map<PackagePaymentDTO>(model);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while processing the status change for package payment ID: {Id}. Error: {Error}", updateActiveDTO.Id, ex.Message);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
