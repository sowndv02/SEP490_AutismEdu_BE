using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Models.DTOs;
using AutismEduConnectSystem.Models.DTOs.CreateDTOs;
using AutismEduConnectSystem.Models.DTOs.UpdateDTOs;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.Utils;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;

namespace AutismEduConnectSystem.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersionNeutral]
    [Authorize(Roles = SD.MANAGER_ROLE)]
    public class DashboardController : ControllerBase
    {
        private readonly IPackagePaymentRepository _packagePaymentRepository;
        private readonly IPaymentHistoryRepository _paymentHistoryRepository;
        private readonly ITutorRepository _tutorRepository;
        private readonly IUserRepository _userRepository;
        private readonly IStudentProfileRepository _studentProfileRepository;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        private readonly ILogger<PackagePaymentController> _logger;
        private readonly IResourceService _resourceService;
        public DashboardController(IPackagePaymentRepository packagePaymentRepository,
            IMapper mapper, IResourceService resourceService,
            ILogger<PackagePaymentController> logger, IPaymentHistoryRepository paymentHistoryRepository, 
            ITutorRepository tutorRepository, IUserRepository userRepository, IStudentProfileRepository studentProfileRepository)
        {
            _response = new APIResponse();
            _mapper = mapper;
            _packagePaymentRepository = packagePaymentRepository;
            _resourceService = resourceService;
            _logger = logger;
            _paymentHistoryRepository = paymentHistoryRepository;
            _tutorRepository = tutorRepository;
            _userRepository = userRepository;
            _studentProfileRepository = studentProfileRepository;
        }



        [HttpGet("TotalParentHaveStudentProfile")]
        public async Task<ActionResult<APIResponse>> GetTotalParentHaveStudentProfile([FromQuery] DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                int total = 0;
                Expression<Func<ApplicationUser, bool>> filter = u => true;

                if (startDate != null)
                {
                    filter = filter.AndAlso(x => x.CreatedDate.Date >= startDate.Value.Date);
                }
                if (endDate != null)
                {
                    filter = filter.AndAlso(x => x.CreatedDate.Date <= endDate.Value.Date);
                }
                total = await _userRepository.GetTotalParentHaveStduentProfileAsync(filter);
                _response.IsSuccess = true;
                _response.Result = total;
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

        [HttpGet("TotalUserInMonth")]
        public async Task<ActionResult<APIResponse>> GetTotalUserInMonthAsync([FromQuery] string userType = SD.PARENT_ROLE, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                int total = 0;
                Expression<Func<ApplicationUser, bool>> filter = u => true;

                if (startDate != null)
                {
                    filter = filter.AndAlso(x => x.CreatedDate.Date >= startDate.Value.Date);
                }
                if (endDate != null)
                {
                    filter = filter.AndAlso(x => x.CreatedDate.Date <= endDate.Value.Date);
                }
                if (!string.IsNullOrEmpty(userType))
                {
                    switch (userType.ToLower())
                    {
                        case "all":
                            total = await _userRepository.GetTotalUserHaveFilterAsync(string.Empty, filter);
                            break;
                        case "parent":
                            total = await _userRepository.GetTotalUserHaveFilterAsync(SD.PARENT_ROLE, filter);
                            break;
                        case "tutor":
                            total = await _userRepository.GetTotalUserHaveFilterAsync(SD.TUTOR_ROLE, filter);
                            break;
                    }
                }
                _response.IsSuccess = true;
                _response.Result = total;
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

        //[HttpGet("TotalRevenues")]
        //public async Task<ActionResult<APIResponse>> GetTotalRevenuesAsync()
        //{
        //    try
        //    {
        //        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //        if (string.IsNullOrEmpty(userId))
        //        {
        //            _logger.LogWarning("Unauthorized access attempt detected.");
        //            _response.IsSuccess = false;
        //            _response.StatusCode = HttpStatusCode.Unauthorized;
        //            _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
        //            return StatusCode((int)HttpStatusCode.Unauthorized, _response);
        //        }
        //        var total = _paymentHistoryRepository.GetTotalRevenues();
        //        _response.IsSuccess = true;
        //        _response.Result = total;
        //        _response.StatusCode = HttpStatusCode.OK;
        //        return Ok(_response);
        //    }
        //    catch (Exception ex)
        //    {
        //        _response.IsSuccess = false;
        //        _logger.LogError("Error occurred while creating an assessment question: {Message}", ex.Message);
        //        _response.StatusCode = HttpStatusCode.InternalServerError;
        //        _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
        //        return StatusCode((int)HttpStatusCode.InternalServerError, _response);
        //    }
        //}

        //[HttpGet("TotalTutor")]
        //public async Task<ActionResult<APIResponse>> GetTotalTutorAsync()
        //{
        //    try
        //    {
        //        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //        if (string.IsNullOrEmpty(userId))
        //        {
        //            _logger.LogWarning("Unauthorized access attempt detected.");
        //            _response.IsSuccess = false;
        //            _response.StatusCode = HttpStatusCode.Unauthorized;
        //            _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
        //            return StatusCode((int)HttpStatusCode.Unauthorized, _response);
        //        }
        //        var total = await _tutorRepository.GetTotalTutor();
        //        _response.IsSuccess = true;
        //        _response.Result = total;
        //        _response.StatusCode = HttpStatusCode.OK;
        //        return Ok(_response);
        //    }
        //    catch (Exception ex)
        //    {
        //        _response.IsSuccess = false;
        //        _logger.LogError("Error occurred while creating an assessment question: {Message}", ex.Message);
        //        _response.StatusCode = HttpStatusCode.InternalServerError;
        //        _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
        //        return StatusCode((int)HttpStatusCode.InternalServerError, _response);
        //    }
        //}


        [HttpGet("PackagePayment")]
        public async Task<ActionResult<APIResponse>> GetAllPackagePaymentAsync([FromQuery]DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                Expression<Func<PaymentHistory, bool>> filter = u => true;

                if (startDate != null)
                {
                    filter = filter.AndAlso(x => x.CreatedDate.Date >= startDate.Value.Date);
                }
                if (endDate != null)
                {
                    filter = filter.AndAlso(x => x.CreatedDate.Date <= endDate.Value.Date);
                }
                var (count, list) = await _packagePaymentRepository.GetAllNotPagingAsync(null, null, null, null, true);
                var result = _mapper.Map<List<PackagePaymentDTO>>(list);
                result.ForEach(x => x.TotalPurchases = _paymentHistoryRepository.GetTotalPaymentHistory(x.Id, filter).GetAwaiter().GetResult());
                _response.IsSuccess = true;
                _response.Result = result.Select(x => new {x.Title, x.TotalPurchases});
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

        [HttpGet("Revenues")]
        public async Task<ActionResult<APIResponse>> GetAllRevenuesAsync([FromQuery] DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized access attempt detected.");
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                Expression<Func<PaymentHistory, bool>> filter = u => true;

                if (startDate != null)
                {
                    filter = filter.AndAlso(x => x.CreatedDate.Date >= startDate.Value.Date);
                }
                if (endDate != null)
                {
                    filter = filter.AndAlso(x => x.CreatedDate.Date <= endDate.Value.Date);
                }
                var (count, list) = await _paymentHistoryRepository.GetAllNotPagingAsync(filter, null, null, null, true);
                var groupedData = list
                    .GroupBy(x => new { x.CreatedDate.Year, x.CreatedDate.Month })
                    .Select(g => new
                    {
                        Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                        TotalPrice = g.Sum(x => x.Amount)
                    })
                    .ToList();
                _response.IsSuccess = true;
                _response.Result = groupedData;
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
