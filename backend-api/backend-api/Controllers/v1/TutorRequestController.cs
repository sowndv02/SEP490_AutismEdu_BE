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
using static backend_api.SD;

namespace backend_api.Controllers.v1
{
    [Route("api/[controller]")]
    [ApiController]
    public class TutorRequestController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ITutorRequestRepository _tutorRequestRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IBlobStorageRepository _blobStorageRepository;
        private readonly ILogger<WorkExperienceController> _logger;
        private readonly IMapper _mapper;
        private readonly FormatString _formatString;
        protected APIResponse _response;
        protected int pageSize = 0;
        public TutorRequestController(IUserRepository userRepository, ITutorRequestRepository tutorRequestRepository,
            ILogger<WorkExperienceController> logger, IBlobStorageRepository blobStorageRepository,
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
            _tutorRequestRepository = tutorRequestRepository;
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? search, string? status = SD.STATUS_ALL, int pageNumber = 1)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int totalCount = 0;
                List<TutorRequest> list = new();
                Expression<Func<TutorRequest, bool>> filter = u => u.TutorId == userId;

                if (!string.IsNullOrEmpty(search))
                {
                    filter = u => !string.IsNullOrEmpty(u.Parent.FullName) && u.Parent.FullName.ToLower().Contains(search.ToLower()) && (filter == null || filter.Compile()(u));
                }

                if (!string.IsNullOrEmpty(status) && status != SD.STATUS_ALL)
                {
                    switch (status.ToLower())
                    {
                        case "approve":
                            var (countApprove, resultApprove) = await _tutorRequestRepository.GetAllAsync(x => x.RequestStatus == Status.APPROVE && (filter == null || filter.Compile()(x)),
                                "ApprovedBy,Curriculums,WorkExperiences,Certificates", pageSize: 5, pageNumber: pageNumber, x => x.CreatedDate, true);
                            list = resultApprove;
                            totalCount = countApprove;
                            break;
                        case "reject":
                            var (countReject, resultReject) = await _tutorRequestRepository.GetAllAsync(x => x.RequestStatus == Status.REJECT && (filter == null || filter.Compile()(x)),
                                "ApprovedBy,Curriculums,WorkExperiences,Certificates", pageSize: 5, pageNumber: pageNumber, x => x.CreatedDate, true);
                            list = resultReject;
                            totalCount = countReject;
                            break;
                        case "pending":
                            var (countPending, resultPending) = await _tutorRequestRepository.GetAllAsync(x => x.RequestStatus == Status.PENDING && (filter == null || filter.Compile()(x)),
                                "ApprovedBy,Curriculums,WorkExperiences,Certificates", pageSize: 5, pageNumber: pageNumber, x => x.CreatedDate, true);
                            list = resultPending;
                            totalCount = countPending;
                            break;
                    }
                }
                else
                {
                    var (count, result) = await _tutorRequestRepository.GetAllAsync(filter,
                               "ApprovedBy,Curriculums,WorkExperiences,Certificates", pageSize: 5, pageNumber: pageNumber, x => x.CreatedDate, true);
                    list = result;
                    totalCount = count;
                }

                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = totalCount };
                _response.Result = _mapper.Map<List<TutorRegistrationRequestDTO>>(list);
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

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<APIResponse>> CreateAsync(TutorRequestCreateDTO tutorRequestCreateDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (tutorRequestCreateDTO == null || string.IsNullOrEmpty(userId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }
                TutorRequest model = _mapper.Map<TutorRequest>(tutorRequestCreateDTO);
                model.ParentId = userId;
                model.CreatedDate = DateTime.Now;
                await _tutorRequestRepository.CreateAsync(model);
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
        //[Authorize(Policy = "UpdateTutorPolicy")]
        public async Task<IActionResult> ApproveOrRejectWorkExperienceRequest(ChangeStatusDTO changeStatusDTO)
        {
            try
            {
                var userId = _userRepository.GetAsync(x => x.Email == SD.ADMIN_EMAIL_DEFAULT).GetAwaiter().GetResult().Id;
                //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                //if (string.IsNullOrEmpty(userId))
                //{
                //    _response.StatusCode = HttpStatusCode.BadRequest;
                //    _response.IsSuccess = false;
                //    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                //    return BadRequest(_response);
                //}

                TutorRequest model = await _tutorRequestRepository.GetAsync(x => x.Id == changeStatusDTO.Id, false, null, null);
                if (changeStatusDTO.StatusChange == (int)Status.APPROVE)
                {
                    model.RequestStatus = Status.APPROVE;
                    model.UpdatedDate = DateTime.Now;
                    await _tutorRequestRepository.UpdateAsync(model);
                    _response.Result = _mapper.Map<TutorRequestDTO>(model);
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    return Ok(_response);
                }
                else if (changeStatusDTO.StatusChange == (int)Status.REJECT)
                {
                    // Handle for reject
                    model.RejectionReason = changeStatusDTO.RejectionReason;
                    model.UpdatedDate = DateTime.Now;
                    await _tutorRequestRepository.UpdateAsync(model);
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    return Ok(_response);

                }

                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
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
