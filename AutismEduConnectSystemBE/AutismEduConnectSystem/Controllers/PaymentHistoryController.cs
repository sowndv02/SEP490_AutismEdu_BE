using AutismEduConnectSystem.DTOs;
using AutismEduConnectSystem.DTOs.CreateDTOs;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.Utils;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;

namespace AutismEduConnectSystem.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersionNeutral]
    public class PaymentHistoryController : ControllerBase
    {
        private readonly IPaymentHistoryRepository _paymentHistoryRepository;
        private readonly IPackagePaymentRepository _packagePaymentRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        protected int pageSize = 0;
        private readonly ILogger<PaymentHistoryController> _logger;
        private readonly IResourceService _resourceService;
        public PaymentHistoryController(IPaymentHistoryRepository paymentHistoryRepository,
            IPackagePaymentRepository packagePaymentRepository,
            IConfiguration configuration, IMapper mapper, IResourceService resourceService,
            ILogger<PaymentHistoryController> logger, IUserRepository userRepository)
        {
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _response = new APIResponse();
            _mapper = mapper;
            _paymentHistoryRepository = paymentHistoryRepository;
            _resourceService = resourceService;
            _logger = logger;
            _packagePaymentRepository = packagePaymentRepository;
            _userRepository = userRepository;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<APIResponse>> CreateAsync(PaymentHistoryCreateDTO createDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.PACKAGE_PAYMENT) };
                    return BadRequest(_response);
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                var newModel = _mapper.Map<PaymentHistory>(createDTO);
                var packagePayment = await _packagePaymentRepository.GetAsync(x => x.Id == createDTO.PackagePaymentId);
                newModel.SubmitterId = userId;
                var latestPaymentHistoryResult = await _paymentHistoryRepository.GetAllAsync(
                    x => x.SubmitterId == userId && x.ExpirationDate.Date >= DateTime.Now.Date,
                    includeProperties: null,
                    pageSize: 1,
                    pageNumber: 1,
                    orderBy: x => x.ExpirationDate,
                    isDesc: true
                );
                var latestPaymentHistory = latestPaymentHistoryResult.list.FirstOrDefault();
                var user = await _userRepository.GetAsync(x =>x.Id == userId);

                int trialDays = 0;
                DateTime trialEndDate = user.CreatedDate.AddDays(30);

                if (trialEndDate >= DateTime.Now)
                {
                    trialDays = (trialEndDate - DateTime.Now).Days;
                }
                if (packagePayment != null && latestPaymentHistory != null && user != null)
                {
                    var additionalMonths = packagePayment.Duration;
                    var remainingDaysFromLastExpiration = (latestPaymentHistory.ExpirationDate - DateTime.Now).Days + trialDays;
                    newModel.ExpirationDate = DateTime.Now.AddMonths(additionalMonths).AddDays(remainingDaysFromLastExpiration);
                }
                else if (packagePayment != null)
                {
                    newModel.ExpirationDate = DateTime.Now.AddMonths(packagePayment.Duration).AddDays(trialDays);
                }

                var result = await _paymentHistoryRepository.CreateAsync(newModel);
                var reuturnModel = await _paymentHistoryRepository.GetAsync(x => x.Id == result.Id, false, "Submitter,PackagePayment", null);
                _response.StatusCode = HttpStatusCode.Created;
                _response.Result = _mapper.Map<PaymentHistoryDTO>(reuturnModel);
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpGet("currentUserPaymentHistory")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetCurrentPaymentHistoryAsync()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                var result = await _paymentHistoryRepository.GetAllNotPagingAsync(x => x.SubmitterId == userId && x.ExpirationDate.Date >= DateTime.Now.Date, "PackagePayment,Submitter", null, x => x.CreatedDate, true);
                if (result.list.FirstOrDefault() != null) _response.Result = _mapper.Map<PaymentHistoryDTO>(result.list.FirstOrDefault());
                _response.IsSuccess = true;
                _response.StatusCode = HttpStatusCode.OK;
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
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? search, DateTime? startDate = null, DateTime? endDate = null, string? orderBy = SD.CREATED_DATE, string? sort = SD.ORDER_DESC, int? packageId = 0, int pageNumber = 1)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
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
                int total = 0;
                if (packageId != 0)
                {
                    filter = filter.AndAlso(x => x.PackagePaymentId == packageId);
                }
                if (!string.IsNullOrEmpty(search))
                {
                    filter = filter.AndAlso(x => x.Description.Contains(search));
                }
                if (startDate != null)
                {
                    filter = filter.AndAlso(x => x.CreatedDate.Date >= startDate.Value.Date);
                }
                if (endDate != null)
                {
                    filter = filter.AndAlso(x => x.CreatedDate.Date <= endDate.Value.Date);
                }
                var result = new List<PaymentHistory>();
                if (userRoles != null && (userRoles.Contains(SD.MANAGER_ROLE) || userRoles.Contains(SD.STAFF_ROLE)))
                {
                    var (count, list) = await _paymentHistoryRepository.GetAllAsync(filter, "Submitter,PackagePayment", pageSize, pageNumber, orderByQuery, isDesc);
                    result = list;
                    total = count;
                }
                else if (userRoles != null && userRoles.Contains(SD.TUTOR_ROLE))
                {

                    filter = filter.AndAlso(x => x.SubmitterId == userId);
                    var (count, list) = await _paymentHistoryRepository.GetAllAsync(filter, "Submitter,PackagePayment", pageSize, pageNumber, orderByQuery, isDesc);
                    result = list;
                    total = count;
                }
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = total };
                _response.Pagination = pagination;
                _response.IsSuccess = true;
                _response.Result = _mapper.Map<List<PaymentHistoryDTO>>(result);
                _response.StatusCode = HttpStatusCode.OK;
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
