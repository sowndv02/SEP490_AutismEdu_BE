using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
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
    public class PaymentHistoryController : ControllerBase
    {
        private readonly IPaymentHistoryRepository _paymentHistoryRepository;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        protected int pageSize = 0;
        private readonly ILogger<PaymentHistoryController> _logger;
        private readonly IResourceService _resourceService;
        public PaymentHistoryController(IPaymentHistoryRepository paymentHistoryRepository,
            IConfiguration configuration, IMapper mapper, IResourceService resourceService,
            ILogger<PaymentHistoryController> logger)
        {
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _response = new APIResponse();
            _mapper = mapper;
            _paymentHistoryRepository = paymentHistoryRepository;
            _resourceService = resourceService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<APIResponse>> CreateAsync(PaymentHistoryCreateDTO createDTO)
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
                var newModel = _mapper.Map<PaymentHistory>(createDTO);
                var result = await _paymentHistoryRepository.CreateAsync(newModel);
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
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? search, DateTime? startDate = null, DateTime? endDate = null, string? orderBy = SD.CREATED_DATE, string? sort = SD.ORDER_DESC, int pageNumber = 1)
        {
            try
            {

                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                Expression<Func<PaymentHistory, bool>> filter = u => true;
                Expression<Func<PaymentHistory, object>> orderByQuery = u => true;

                bool isDesc = !string.IsNullOrEmpty(sort) && sort == SD.ORDER_DESC;

                if (orderBy != null)
                {
                    switch (orderBy)
                    {
                        case SD.CREATED_DATE:
                            orderByQuery = x => x.CreatedDate;
                            break;
                        default:
                            orderByQuery = x => x.CreatedDate;
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(search))
                {
                    filter = filter.AndAlso(x => x.Description.Contains(search));
                }
                if(startDate != null)
                {
                    filter = filter.AndAlso(x => x.CreatedDate >= startDate);
                }
                if (endDate != null) 
                {
                    filter = filter.AndAlso(x => x.CreatedDate <= endDate);
                } 
                var result = new List<PaymentHistory>();
                if (userRoles != null && userRoles.Contains(SD.MANAGER_ROLE))
                {
                    var (count, list) = await _paymentHistoryRepository.GetAllAsync(filter, "Submitter,PackagePayment", pageSize, pageNumber, x => x.CreatedDate, isDesc);
                    result = list;
                }
                else if(userRoles != null && userRoles.Contains(SD.TUTOR_ROLE))
                {
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    filter = filter.AndAlso(x => x.SubmitterId == userId);
                    var (count, list) = await _paymentHistoryRepository.GetAllAsync(filter, "Submitter,PackagePayment", pageSize, pageNumber, x => x.CreatedDate, isDesc);
                    result = list;
                }
                _response.IsSuccess = true;
                _response.Result = _mapper.Map<List<PaymentHistoryDTO>>(result);
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


    }
}
