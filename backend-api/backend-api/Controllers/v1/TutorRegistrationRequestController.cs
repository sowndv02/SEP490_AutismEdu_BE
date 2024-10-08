using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models.DTOs.UpdateDTOs;
using backend_api.Repository.IRepository;
using backend_api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Net;
using static backend_api.SD;

namespace backend_api.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    //[Authorize]
    public class TutorRegistrationRequestController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ITutorRepository _tutorRepository;
        private readonly IEmailSender _emailSender;
        private readonly ITutorRegistrationRequestRepository _tutorRegistrationRequestRepository;
        private readonly ICurriculumRepository _curriculumRepository;
        private readonly IWorkExperienceRepository _workExperienceRepository;
        private readonly ICertificateMediaRepository _certificateMediaRepository;
        private readonly ICertificateRepository _certificateRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IBlobStorageRepository _blobStorageRepository;
        private readonly ILogger<TutorController> _logger;
        private readonly IMapper _mapper;
        private readonly FormatString _formatString;
        protected APIResponse _response;
        protected int pageSize = 0;
        public TutorRegistrationRequestController(IUserRepository userRepository, ITutorRepository tutorRepository,
            ILogger<TutorController> logger, IBlobStorageRepository blobStorageRepository,
            IMapper mapper, IConfiguration configuration, IRoleRepository roleRepository,
            FormatString formatString, IWorkExperienceRepository workExperienceRepository,
            ICertificateRepository certificateRepository, ICertificateMediaRepository certificateMediaRepository,
            ITutorRegistrationRequestRepository tutorRegistrationRequestRepository, ICurriculumRepository curriculumRepository,
            IEmailSender emailSender)
        {
            _emailSender = emailSender;
            _curriculumRepository = curriculumRepository;
            _formatString = formatString;
            _roleRepository = roleRepository;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _response = new APIResponse();
            _mapper = mapper;
            _blobStorageRepository = blobStorageRepository;
            _logger = logger;
            _userRepository = userRepository;
            _tutorRepository = tutorRepository;
            _workExperienceRepository = workExperienceRepository;
            _certificateRepository = certificateRepository;
            _certificateMediaRepository = certificateMediaRepository;
            _tutorRegistrationRequestRepository = tutorRegistrationRequestRepository;
        }


        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<APIResponse>> CreateTutorRegistrationRequestAsync([FromForm] TutorRegistrationRequestCreateDTO tutorRegistrationRequestCreateDTO)
        {
            try
            {
                if (tutorRegistrationRequestCreateDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { $"Bad request!" };
                    return BadRequest(_response);
                }
                if (_tutorRegistrationRequestRepository.GetAsync(x => x.Email.Equals(tutorRegistrationRequestCreateDTO.Email) && (x.RequestStatus == Status.PENDING || x.RequestStatus == Status.APPROVE), true, null).GetAwaiter().GetResult() != null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.TUTOR_REGISTER_REQUEST_EXIST_OR_IS_TUTOR };
                    return BadRequest(_response);
                }
                if (tutorRegistrationRequestCreateDTO.StartAge > tutorRegistrationRequestCreateDTO.EndAge)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }
                tutorRegistrationRequestCreateDTO.FullName = _formatString.FormatStringFormalName(tutorRegistrationRequestCreateDTO.FullName);
                TutorRegistrationRequest model = _mapper.Map<TutorRegistrationRequest>(tutorRegistrationRequestCreateDTO);

                if (tutorRegistrationRequestCreateDTO.Image != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(tutorRegistrationRequestCreateDTO.Image.FileName);
                    using var stream = tutorRegistrationRequestCreateDTO.Image.OpenReadStream();
                    model.ImageUrl = await _blobStorageRepository.Upload(stream, fileName);
                }

                model = await _tutorRegistrationRequestRepository.CreateAsync(model);

                // Handle certificate media uploads
                if (tutorRegistrationRequestCreateDTO.Certificates != null && model.Certificates != null)
                {
                    for (int i = 0; i < tutorRegistrationRequestCreateDTO.Certificates.Count; i++)
                    {
                        var certificateDTO = tutorRegistrationRequestCreateDTO.Certificates[i];
                        var certificate = model.Certificates[i];

                        if (certificateDTO.Medias != null && certificateDTO.Medias.Count > 0)
                        {
                            foreach (var media in certificateDTO.Medias)
                            {
                                string mediaFileName = Guid.NewGuid().ToString() + Path.GetExtension(media.FileName);
                                using var mediaStream = media.OpenReadStream();
                                string mediaUrl = await _blobStorageRepository.Upload(mediaStream, mediaFileName);

                                CertificateMedia certificateMedia = new CertificateMedia
                                {
                                    CertificateId = certificate.Id,
                                    UrlPath = mediaUrl
                                };
                                await _certificateMediaRepository.CreateAsync(certificateMedia);
                            }
                        }
                    }
                }
                // TODO: Send email
                var subject = "Xác nhận Đăng ký Gia sư Dạy Trẻ Tự Kỷ - Đang Chờ Duyệt";
                var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "TutorRegistrationRequestTemplate.cshtml");
                var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);
                var htmlMessage = templateContent
                    .Replace("@Model.FullName", model.FullName)
                    .Replace("@Model.Email", model.Email)
                    .Replace("@Model.RegistrationDate", model.CreatedDate.ToString("dd/MM/yyyy"));
                await _emailSender.SendEmailAsync(model.Email, subject, htmlMessage);
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

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? search, string? status = SD.STATUS_ALL, int pageNumber = 1)
        {
            try
            {
                int totalCount = 0;
                List<TutorRegistrationRequest> list = new();
                Expression<Func<TutorRegistrationRequest, bool>> filter = u => true;

                if (!string.IsNullOrEmpty(search))
                {
                    filter = u => !string.IsNullOrEmpty(u.FullName) && u.FullName.ToLower().Contains(search.ToLower());
                }

                if (!string.IsNullOrEmpty(status) && status != SD.STATUS_ALL)
                {
                    switch (status.ToLower())
                    {
                        case "approve":
                            var (countApprove, resultApprove) = await _tutorRegistrationRequestRepository.GetAllAsync(x => x.RequestStatus == Status.APPROVE && (filter == null || filter.Compile()(x)),
                                "ApprovedBy,Curriculums,WorkExperiences,Certificates", pageSize: pageSize, pageNumber: pageNumber, x => x.CreatedDate, true);
                            list = resultApprove;
                            totalCount = countApprove;
                            break;
                        case "reject":
                            var (countReject, resultReject) = await _tutorRegistrationRequestRepository.GetAllAsync(x => x.RequestStatus == Status.REJECT && (filter == null || filter.Compile()(x)),
                                "ApprovedBy,Curriculums,WorkExperiences,Certificates", pageSize: pageSize, pageNumber: pageNumber, x => x.CreatedDate, true);
                            list = resultReject;
                            totalCount = countReject;
                            break;
                        case "pending":
                            var (countPending, resultPending) = await _tutorRegistrationRequestRepository.GetAllAsync(x => x.RequestStatus == Status.PENDING && (filter == null || filter.Compile()(x)),
                                "ApprovedBy,Curriculums,WorkExperiences,Certificates", pageSize: pageSize, pageNumber: pageNumber, x => x.CreatedDate, true);
                            list = resultPending;
                            totalCount = countPending;
                            break;
                    }
                }
                else
                {
                    var (count, result) = await _tutorRegistrationRequestRepository.GetAllAsync(filter,
                                "ApprovedBy,Curriculums,WorkExperiences,Certificates", pageSize: pageSize, pageNumber: pageNumber, x => x.CreatedDate, true);
                    list = result;
                    totalCount = count;
                }

                foreach (var item in list)
                {
                    foreach (var certificate in item.Certificates)
                    {
                        var (countMedias, medias) = await _certificateMediaRepository.GetAllNotPagingAsync(x => x.CertificateId == certificate.Id, includeProperties: null, excludeProperties: null);
                        certificate.CertificateMedias = medias;
                    }
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

        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse>> GetById(string id)
        {
            try
            {
                var result = await _tutorRepository.GetAsync(x => x.UserId == id, false, "User", null);
                _response.Result = _mapper.Map<TutorDTO>(result);
                _response.StatusCode = HttpStatusCode.OK;
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
        public async Task<IActionResult> ApproveOrRejectTutorRegistrationRequest(ChangeStatusDTO tutorRegistrationRequestChange)
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
                if (tutorRegistrationRequestChange.StatusChange == (int)Status.PENDING)
                {
                    _response.ErrorMessages = new List<string>() { SD.TUTOR_UPDATE_STATUS_IS_PENDING };
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }
                TutorRegistrationRequest model = await _tutorRegistrationRequestRepository.GetAsync(x => x.Id == tutorRegistrationRequestChange.Id, false, "ApprovedBy,Curriculums,WorkExperiences,Certificates", null);
                if (tutorRegistrationRequestChange.StatusChange == (int)Status.APPROVE)
                {
                    string passsword = PasswordGenerator.GeneratePassword();
                    // Create user
                    var user = await _userRepository.CreateAsync(new ApplicationUser
                    {
                        Email = model.Email,
                        Address = model.Address,
                        FullName = model.FullName,
                        PhoneNumber = model.PhoneNumber,
                        EmailConfirmed = true,
                        IsLockedOut = false,
                        ImageUrl = model.ImageUrl,
                        CreatedDate = DateTime.Now,
                        UserName = model.Email,
                        UserType = SD.APPLICATION_USER,
                        LockoutEnabled = true,
                        RoleIds = new List<string>() { _roleRepository.GetByNameAsync(SD.TUTOR_ROLE).GetAwaiter().GetResult().Id }
                    }, passsword);

                    // Create tutor profile
                    var tutor = await _tutorRepository.CreateAsync(new Tutor()
                    {
                        UserId = user.Id,
                        Price = model.Price,
                        AboutMe = model.Description,
                        DateOfBirth = model.DateOfBirth,
                        StartAge = model.StartAge,
                        EndAge = model.EndAge,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    });

                    // Update status curriculum
                    if (model.Curriculums != null)
                    {
                        var curiculums = model.Curriculums.Where(x => x.RequestStatus == Status.PENDING).ToList();
                        foreach (var item in curiculums)
                        {
                            item.ApprovedId = userId;
                            item.UpdatedDate = DateTime.Now;
                            item.SubmiterId = tutor.UserId;
                            await _curriculumRepository.UpdateAsync(item);
                        }
                    }

                    // Update status certificate except certificate have status is reject
                    if (model.Certificates != null)
                    {
                        var certificates = model.Certificates.Where(x => x.RequestStatus == Status.PENDING).ToList();
                        foreach (var cert in certificates)
                        {
                            cert.SubmiterId = tutor.UserId;
                            cert.ApprovedId = userId;
                            cert.UpdatedDate = DateTime.Now;
                            await _certificateRepository.UpdateAsync(cert);
                        }
                    }

                    // Update status work experience
                    if (model.WorkExperiences != null)
                    {
                        var workExperiences = model.WorkExperiences.Where(x => x.RequestStatus == Status.PENDING).ToList();
                        foreach (var workExperience in workExperiences)
                        {
                            workExperience.SubmiterId = tutor.UserId;
                            workExperience.ApprovedId = userId;
                            workExperience.UpdatedDate = DateTime.Now;
                            await _workExperienceRepository.UpdateAsync(workExperience);
                        }
                    }

                    model.RequestStatus = Status.APPROVE;
                    model.UpdatedDate = DateTime.Now;
                    model.ApprovedId = userId;
                    await _tutorRegistrationRequestRepository.UpdateAsync(model);
                    // TODO: Send mail
                    var subject = "Thông báo Chấp nhận Đơn Đăng ký Gia sư Dạy Trẻ Tự Kỷ";

                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "AcceptedTutorRegistrationRequest.cshtml");
                    var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);

                    var htmlMessage = templateContent
                        .Replace("@Model.FullName", model.FullName)
                        .Replace("@Model.Username", model.Email)
                        .Replace("@Model.Password", passsword)
                        .Replace("@Model.LoginUrl", SD.URL_FE_TUTOR_LOGIN);


                    await _emailSender.SendEmailAsync(model.Email, subject, htmlMessage);

                    _response.Result = _mapper.Map<TutorDTO>(tutor);
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    return Ok(_response);
                }
                else if (tutorRegistrationRequestChange.StatusChange == (int)Status.REJECT)
                {
                    // Handle for reject
                    model.RejectionReason = tutorRegistrationRequestChange.RejectionReason;
                    model.UpdatedDate = DateTime.Now;
                    model.ApprovedId = userId;
                    await _tutorRegistrationRequestRepository.UpdateAsync(model);

                    // Reject certificate
                    if (model.Certificates != null)
                    {
                        foreach (var cert in model.Certificates)
                        {
                            cert.ApprovedId = userId;
                            cert.UpdatedDate = DateTime.Now;
                            cert.RejectionReason = tutorRegistrationRequestChange.RejectionReason;
                            cert.RequestStatus = Status.REJECT;
                            await _certificateRepository.UpdateAsync(cert);
                        }
                    }

                    // Reject curriculum
                    if (model.Curriculums != null)
                    {
                        foreach (var item in model.Curriculums)
                        {
                            item.ApprovedId = userId;
                            item.UpdatedDate = DateTime.Now;
                            item.RejectionReason = tutorRegistrationRequestChange.RejectionReason;
                            item.RequestStatus = Status.REJECT;
                            await _curriculumRepository.UpdateAsync(item);
                        }
                    }

                    if (model.WorkExperiences != null)
                    {
                        foreach (var item in model.WorkExperiences)
                        {
                            item.ApprovedId = userId;
                            item.UpdatedDate = DateTime.Now;
                            item.RejectionReason = tutorRegistrationRequestChange.RejectionReason;
                            item.RequestStatus = Status.REJECT;
                            await _workExperienceRepository.UpdateAsync(item);
                        }
                    }
                    // TODO: Send mail

                    var subject = "Thông báo Từ chối Đơn Đăng ký Gia sư và Hướng dẫn Tạo Đơn Mới";
                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "RejectTutorRegistrationRequest.cshtml");
                    var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);
                    var htmlMessage = templateContent
                        .Replace("@Model.FullName", model.FullName)
                        .Replace("@Model.RejectionReason", model.RejectionReason ?? "Không có lý do cụ thể.");

                    await _emailSender.SendEmailAsync(model.Email, subject, htmlMessage);
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.Result = _mapper.Map<TutorRegistrationRequestDTO>(model);
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
