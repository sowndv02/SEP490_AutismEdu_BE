using AutismEduConnectSystem.DTOs;
using AutismEduConnectSystem.DTOs.CreateDTOs;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.SignalR;
using AutismEduConnectSystem.Utils;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using static AutismEduConnectSystem.SD;

namespace AutismEduConnectSystem.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class StudentProfileController : ControllerBase
    {
        private readonly IStudentProfileRepository _studentProfileRepository;
        private readonly IScheduleTimeSlotRepository _scheduleTimeSlotRepository;
        private readonly IInitialAssessmentResultRepository _initialAssessmentResultRepository;
        private readonly IAssessmentQuestionRepository _assessmentQuestionRepository;
        private readonly IChildInformationRepository _childInfoRepository;
        private readonly ITutorRequestRepository _tutorRequestRepository;
        private readonly ITutorRepository _tutorRepository;
        private readonly IEmailSender _messageBus;
        private string queueName = string.Empty;
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IBlobStorageRepository _blobStorageRepository;
        private readonly IScheduleRepository _scheduleRepository;
        private readonly IResourceService _resourceService;
        private readonly ILogger<StudentProfileController> _logger;
        private readonly INotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationHub> _hubContext;

        protected APIResponse _response;
        private readonly IMapper _mapper;
        protected int pageSize = 0;

        public StudentProfileController(IStudentProfileRepository studentProfileRepository, IAssessmentQuestionRepository assessmentQuestionRepository,
            IScheduleTimeSlotRepository scheduleTimeSlotRepository, IInitialAssessmentResultRepository initialAssessmentResultRepository
            , IChildInformationRepository childInfoRepository, ITutorRequestRepository tutorRequestRepository,
            IMapper mapper, IConfiguration configuration, ITutorRepository tutorRepository, IEmailSender messageBus,
            IUserRepository userRepository, IRoleRepository roleRepository, IBlobStorageRepository blobStorageRepository
            , IScheduleRepository scheduleRepository, IResourceService resourceService, ILogger<StudentProfileController> logger,
            INotificationRepository notificationRepository, IHubContext<NotificationHub> hubContext)
        {
            _studentProfileRepository = studentProfileRepository;
            _assessmentQuestionRepository = assessmentQuestionRepository;
            _scheduleTimeSlotRepository = scheduleTimeSlotRepository;
            _initialAssessmentResultRepository = initialAssessmentResultRepository;
            _childInfoRepository = childInfoRepository;
            queueName = configuration["RabbitMQSettings:QueueName"];
            _tutorRequestRepository = tutorRequestRepository;
            _response = new APIResponse();
            _mapper = mapper;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _tutorRepository = tutorRepository;
            _messageBus = messageBus;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _blobStorageRepository = blobStorageRepository;
            _scheduleRepository = scheduleRepository;
            _resourceService = resourceService;
            _logger = logger;
            _notificationRepository = notificationRepository;
            _hubContext = hubContext;
        }

        [HttpPost]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> CreateAsync([FromForm] StudentProfileCreateDTO createDTO)
        {
            try
            {

                var tutorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(tutorId))
                {

                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }

                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                if (userRoles == null || (!userRoles.Contains(SD.TUTOR_ROLE)))
                {

                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }

                if (createDTO == null || !ModelState.IsValid)
                {
                    _logger.LogWarning("Received empty createDTO from tutor {TutorId}", tutorId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.STUDENT_PROFILE) };
                    return BadRequest(_response);
                }

                List<ScheduleTimeSlot> scheduleTimeSlot = _mapper.Map<List<ScheduleTimeSlot>>(createDTO.ScheduleTimeSlots);
                foreach (var slot in scheduleTimeSlot)
                {

                    slot.AppliedDate = DateTime.Today;

                    var isTimeSlotDuplicate = scheduleTimeSlot.Where(x => x != slot
                                                                       && x.Weekday == slot.Weekday
                                                                       && !(slot.To <= x.From || slot.From >= x.To)).FirstOrDefault();
                    if (isTimeSlotDuplicate == null)
                    {
                        if (slot.From >= slot.To)
                        {
                            _logger.LogWarning("Invalid time slot detected: From time ({From}) is greater than or equal to To time ({To}) for the time slot {Weekday}.",
                                slot.From.ToString(@"hh\:mm"),
                                slot.To.ToString(@"hh\:mm"),
                                slot.Weekday);
                            _response.StatusCode = HttpStatusCode.BadRequest;
                            _response.IsSuccess = false;
                            _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.TIME_SLOT) };
                            return BadRequest(_response);
                        }

                        isTimeSlotDuplicate = await _scheduleTimeSlotRepository.GetAsync(x => x.Weekday == slot.Weekday
                                                                                           && x.StudentProfile.TutorId.Equals(tutorId)
                                                                                           && !(slot.To <= x.From || slot.From >= x.To)
                                                                                           && !x.IsDeleted
                                                                                           && (x.StudentProfile.Status == SD.StudentProfileStatus.Pending
                                                                                           || x.StudentProfile.Status == SD.StudentProfileStatus.Teaching)
                                                                                           , true, "StudentProfile");
                        if (isTimeSlotDuplicate != null)
                        {
                            _logger.LogWarning("Duplicate time slot detected: From time ({From}) to To time ({To}) already exists for the time slot on {Weekday}.",
                                isTimeSlotDuplicate.From.ToString(@"hh\:mm"),
                                isTimeSlotDuplicate.To.ToString(@"hh\:mm"),
                                isTimeSlotDuplicate.Weekday);
                            _response.StatusCode = HttpStatusCode.BadRequest;
                            _response.IsSuccess = false;
                            _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.TIMESLOT_DUPLICATED_MESSAGE, SD.TIME_SLOT, isTimeSlotDuplicate.From.ToString(@"hh\:mm"), isTimeSlotDuplicate.To.ToString(@"hh\:mm")) };
                            return BadRequest(_response);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Duplicate time slot detected: From time ({From}) to To time ({To}) already exists.",
                            isTimeSlotDuplicate.From.ToString(@"hh\:mm"),
                            isTimeSlotDuplicate.To.ToString(@"hh\:mm"));
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.TIMESLOT_DUPLICATED_MESSAGE, SD.TIME_SLOT, isTimeSlotDuplicate.From.ToString(@"hh\:mm"), isTimeSlotDuplicate.To.ToString(@"hh\:mm")) };
                        return BadRequest(_response);
                    }
                }

                StudentProfile model = _mapper.Map<StudentProfile>(createDTO);
                model.CreatedDate = DateTime.Now;
                model.TutorId = tutorId;
                model.ScheduleTimeSlots = scheduleTimeSlot;
                model.Status = SD.StudentProfileStatus.Teaching;

                foreach (var assessment in model.InitialAndFinalAssessmentResults)
                {
                    assessment.isInitialAssessment = true;
                }

                if (!string.IsNullOrEmpty(createDTO.Email) &&
                    !string.IsNullOrEmpty(createDTO.ParentFullName) &&
                    !string.IsNullOrEmpty(createDTO.Address) &&
                    !string.IsNullOrEmpty(createDTO.PhoneNumber) &&
                    !string.IsNullOrEmpty(createDTO.ChildName) &&
                    createDTO.isMale != null &&
                    !string.IsNullOrEmpty(createDTO.BirthDate.ToString()) &&
                    createDTO.Media != null)
                {
                    if (createDTO.TutorRequestId > 0)
                    {
                        _logger.LogWarning("Invalid request: TutorRequestId ({TutorRequestId}) should not be greater than 0.", createDTO.TutorRequestId);
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.STUDENT_PROFILE) };
                        return BadRequest(_response);
                    }

                    // Tao account parent
                    var parentEmailExist = await _userRepository.GetAsync(x => x.Email.Equals(createDTO.Email));
                    if (parentEmailExist != null)
                    {
                        _logger.LogWarning("Duplicate email attempt: The email ({Email}) already exists.", parentEmailExist.Email);
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.EMAIL) };
                        return BadRequest(_response);
                    }

                    string passsword = PasswordGenerator.GeneratePassword();
                    var parent = await _userRepository.CreateAsync(new ApplicationUser
                    {
                        Email = createDTO.Email,
                        Address = createDTO.Address,
                        FullName = createDTO.ParentFullName,
                        PhoneNumber = createDTO.PhoneNumber,
                        EmailConfirmed = true,
                        IsLockedOut = false,
                        ImageUrl = SD.URL_IMAGE_DEFAULT_BLOB,
                        CreatedDate = DateTime.Now,
                        UserName = createDTO.Email,
                        UserType = SD.APPLICATION_USER,
                        LockoutEnabled = true,
                        RoleId = _roleRepository.GetByNameAsync(SD.PARENT_ROLE).GetAwaiter().GetResult().Id
                    }, passsword);
                    if (parent == null)
                    {
                        _response.StatusCode = HttpStatusCode.InternalServerError;
                        _response.IsSuccess = false;
                        _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                        return StatusCode((int)HttpStatusCode.InternalServerError, _response);
                    }
                    var subject = "Thông báo ";

                    var templatePathSendEmailForParent = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "CreateParentAndChild.cshtml");
                    //if (!System.IO.File.Exists(templatePathSendEmailForParent))
                    //{
                    //    _response.StatusCode = HttpStatusCode.InternalServerError;
                    //    _response.IsSuccess = false;
                    //    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                    //    return StatusCode((int)HttpStatusCode.InternalServerError, _response);
                    //}
                    if (System.IO.File.Exists(templatePathSendEmailForParent))
                    {
                        var templateContent = await System.IO.File.ReadAllTextAsync(templatePathSendEmailForParent);
                        var htmlMessage = templateContent
                            .Replace("@Model.FullName", parent.FullName)
                            .Replace("@Model.Username", parent.Email)
                            .Replace("@Model.Password", passsword)
                            .Replace("@Model.LoginUrl", SD.URL_FE_PARENT_LOGIN)
                            .Replace("@Model.Mail", SD.MAIL)
                            .Replace("@Model.Phone", SD.PHONE_NUMBER)
                            .Replace("@Model.WebsiteURL", SD.URL_FE);
                        await _messageBus.SendEmailAsync(parent.Email, subject, htmlMessage);
                    }
                       
                    //_messageBus.SendMessage(new EmailLogger()
                    //{
                    //    UserId = parent.Id,
                    //    Email = parent.Email,
                    //    Subject = subject,
                    //    Message = htmlMessage
                    //}, queueName);
                    

                    // Tao child
                    using var mediaStream = createDTO.Media.OpenReadStream();
                    string mediaUrl = await _blobStorageRepository.Upload(mediaStream, string.Concat(Guid.NewGuid().ToString(), Path.GetExtension(createDTO.Media.FileName)));

                    var childInformation = await _childInfoRepository.CreateAsync(new ChildInformation()
                    {
                        ParentId = parent.Id,
                        Name = createDTO.ChildName,
                        isMale = (bool)createDTO.isMale,
                        ImageUrlPath = mediaUrl,
                        BirthDate = createDTO.BirthDate,
                        CreatedDate = DateTime.Now
                    });
                    if (childInformation == null)
                    {
                        _response.StatusCode = HttpStatusCode.InternalServerError;
                        _response.IsSuccess = false;
                        _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                        return StatusCode((int)HttpStatusCode.InternalServerError, _response);
                    }
                    model.ChildId = childInformation.Id;
                }
                else if (createDTO.ChildId <= 0)
                {
                    _logger.LogWarning("Invalid ChildId: The ChildId ({ChildId}) is missing or invalid.", createDTO.ChildId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.MISSING_2_INFORMATIONS, SD.PARENT, SD.CHILD) };
                    return BadRequest(_response);
                }

                var childTutorExist = await _studentProfileRepository.GetAsync(x => x.ChildId == model.ChildId
                                                && x.TutorId.Equals(tutorId) && (x.Status == SD.StudentProfileStatus.Teaching || x.Status == SD.StudentProfileStatus.Pending));

                if (childTutorExist != null)
                {
                    _logger.LogWarning("Duplicate student profile detected: The student profile already exists for the child with ID {ChildId}.", childTutorExist.ChildId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.STUDENT_PROFILE) };
                    return BadRequest(_response);
                }

                var child = await _childInfoRepository.GetAsync(x => x.Id == model.ChildId, true, "Parent");
                if (child == null)
                {
                    _logger.LogError("Child information not found: No child found with the given criteria.");
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.CHILD_INFO) };
                    return NotFound(_response);
                }
                model.Child = child;

                if (!string.IsNullOrEmpty(child.Name))
                {
                    string[] names = child.Name.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                    for(int i = 0; i < (names.Length > 6 ? 6:names.Length); i++)
                    {
                        model.StudentCode += names[i].ToUpper().ElementAt(0);
                    }
                }

                model.StudentCode += model.ChildId;
                model.Tutor = await _tutorRepository.GetAsync(x => x.TutorId.Equals(tutorId), true, "User");
                model = await _studentProfileRepository.CreateAsync(model);

                // Notification
                if (model.Tutor != null && model.Tutor.User != null)
                {
                    var connectionId = NotificationHub.GetConnectionIdByUserId(child.ParentId);
                    var notfication = new Notification()
                    {
                        ReceiverId = child.ParentId,
                        Message = _resourceService.GetString(SD.CREATE_STUDENT_PROFILE_PARENT_NOTIFICATION, model.Tutor?.User.FullName),
                        UrlDetail = string.Concat(SD.URL_FE, SD.URL_FE_PARENT_STUDENT_PROFILE_LIST, model.Id),
                        IsRead = false,
                        CreatedDate = DateTime.Now
                    };
                    var notificationResult = await _notificationRepository.CreateAsync(notfication);
                    if (!string.IsNullOrEmpty(connectionId))
                    {
                        await _hubContext.Clients.Client(connectionId).SendAsync($"Notifications-{child.ParentId}", _mapper.Map<NotificationDTO>(notificationResult));
                    }
                }

                await GenerateSchedule(model);

                var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "CreateStudentProfileTemplate.cshtml");
                if (System.IO.File.Exists(templatePath) && child.Parent != null && model.Tutor != null && model.Tutor.User != null)
                {
                    var subject = "Thông Báo Hồ Sơ Học Sinh Đã Được Tạo";
                    var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);
                    var htmlMessage = templateContent
                        .Replace("@Model.ParentName", child.Parent.FullName)
                        .Replace("@Model.TutorName", model.Tutor.User.FullName)
                        .Replace("@Model.StudentName", child.Name)
                        .Replace("@Model.Email", child.Parent.Email)
                        .Replace("@Model.Url", string.Concat(SD.URL_FE, SD.URL_FE_PARENT_STUDENT_PROFILE_LIST, model.Id))
                        .Replace("@Model.ProfileCreationDate", model.CreatedDate.ToString("dd/MM/yyyy"))
                        .Replace("@Model.Mail", SD.MAIL)
                        .Replace("@Model.Phone", SD.PHONE_NUMBER)
                        .Replace("@Model.WebsiteURL", SD.URL_FE);
                    
                    //_messageBus.SendMessage(new EmailLogger()
                    //{
                    //    UserId = child.ParentId,
                    //    Email = child.Parent.Email,
                    //    Subject = subject,
                    //    Message = htmlMessage
                    //}, queueName);
                    await _messageBus.SendEmailAsync(child.Parent.Email, subject, htmlMessage);
                }

                if (createDTO.TutorRequestId > 0)
                {
                    var tutorRequest = await _tutorRequestRepository.GetAsync(x => x.Id == createDTO.TutorRequestId);
                    tutorRequest.HasStudentProfile = true;
                    await _tutorRequestRepository.UpdateAsync(tutorRequest);
                }

                _response.StatusCode = HttpStatusCode.Created;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating student profile for tutor {TutorId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        private async Task GenerateSchedule(StudentProfile model)
        {
            //Generate current week schedule
            foreach (var timeslot in model.ScheduleTimeSlots)
            {
                DateTime today = DateTime.Now.Date;
                if ((int)today.DayOfWeek > timeslot.Weekday && timeslot.Weekday != 0)
                {
                    continue;
                }
                int daysUntilTargetDay = (timeslot.Weekday - (int)today.DayOfWeek + 7) % 7;

                DateTime targetDate = today.AddDays(daysUntilTargetDay);
                if (targetDate.Date.Add(timeslot.From) < DateTime.Now)
                {
                    continue;
                }
                var schedule = new Schedule()
                {
                    TutorId = model.TutorId,
                    AttendanceStatus = SD.AttendanceStatus.NOT_YET,
                    ScheduleDate = targetDate,
                    StudentProfileId = model.Id,
                    CreatedDate = DateTime.Now,
                    PassingStatus = SD.PassingStatus.NOT_YET,
                    UpdatedDate = null,
                    Start = timeslot.From,
                    End = timeslot.To,
                    ScheduleTimeSlotId = timeslot.Id,
                    Note = ""
                };
                await _scheduleRepository.CreateAsync(schedule);
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? status = SD.STATUS_ALL, string? sort = SD.ORDER_DESC, int pageNumber = 1)
        {
            try
            {
                Expression<Func<StudentProfile, bool>> filter = u => true;
                bool isDesc = sort != null && sort == SD.ORDER_DESC;

                var tutorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(tutorId))
                {

                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                filter = filter.AndAlso(x => x.TutorId.Equals(tutorId));

                if (!string.IsNullOrEmpty(status) && status != SD.STATUS_ALL)
                {
                    switch (status.ToLower())
                    {
                        case "pending":
                            filter = filter.AndAlso(x => x.Status == SD.StudentProfileStatus.Pending);
                            break;
                        case "reject":
                            filter = filter.AndAlso(x => x.Status == SD.StudentProfileStatus.Reject);
                            break;
                        case "teaching":
                            filter = filter.AndAlso(x => x.Status == SD.StudentProfileStatus.Teaching);
                            break;
                        case "stop":
                            filter = filter.AndAlso(x => x.Status == SD.StudentProfileStatus.Stop);
                            break;
                    }
                }
                var (count, result) = await _studentProfileRepository.GetAllWithIncludeAsync(filter,
                                "Child", pageSize: pageSize, pageNumber: pageNumber, x => x.CreatedDate, isDesc);

                var studentProfiles = _mapper.Map<List<StudentProfileDTO>>(result);
                foreach (var profile in studentProfiles)
                {
                    var parent = await _childInfoRepository.GetAsync(x => x.Id == profile.ChildId, true, "Parent");
                    if (parent != null && parent.Parent != null)
                    {
                        profile.Address = parent.Parent.Address;
                        profile.PhoneNumber = parent.Parent.PhoneNumber;
                    }
                }

                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = count };
                _response.Result = studentProfiles;
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching student profiles.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet("GetAllScheduleTimeSlot")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllScheduleTimeSlot()
        {
            try
            {
                var tutorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(tutorId))
                {

                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                var scheduleTimeSlots = await _studentProfileRepository.GetAllNotPagingAsync(x => x.TutorId.Equals(tutorId)
                && (x.Status == SD.StudentProfileStatus.Teaching || x.Status == SD.StudentProfileStatus.Pending), "ScheduleTimeSlots,Child");

                _response.Result = _mapper.Map<List<GetAllStudentProfileTimeSlotDTO>>(scheduleTimeSlots.list);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching schedule time slots.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet("GetAllChildStudentProfile")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllChildStudentProfile([FromQuery] string? status = SD.STATUS_ALL, int pageNumber = 1)
        {
            try
            {
                var parentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(parentId))
                {

                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }
                var childs = await _childInfoRepository.GetAllNotPagingAsync(x => x.ParentId.Equals(parentId));

                if (childs.list == null || !childs.list.Any())
                {
                    _logger.LogInformation("No children found for the parent. Returning empty result.");
                    _response.Result = new List<StudentProfile>();
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    return Ok(_response);
                }

                List<StudentProfile> studentProfiles = new List<StudentProfile>();

                foreach (var child in childs.list)
                {
                    Expression<Func<StudentProfile, bool>> filter = u => true;
                    filter = u => u.ChildId.Equals(child.Id);
                    if (!string.IsNullOrEmpty(status) && status != SD.STATUS_ALL)
                    {
                        switch (status.ToLower())
                        {
                            case "pending":
                                filter = filter.AndAlso(x => x.Status == SD.StudentProfileStatus.Pending);
                                break;
                            case "reject":
                                filter = filter.AndAlso(x => x.Status == SD.StudentProfileStatus.Reject);
                                break;
                            case "teaching":
                                filter = filter.AndAlso(x => x.Status == SD.StudentProfileStatus.Teaching);
                                break;
                            case "stop":
                                filter = filter.AndAlso(x => x.Status == SD.StudentProfileStatus.Stop);
                                break;
                        }
                    }
                    var profile = await _studentProfileRepository.GetAllNotPagingAsync(filter, "Child,Tutor");
                    if (profile.list != null)
                    {
                        foreach(var studentProfile in profile.list)
                        {
                            studentProfile.Tutor = await _tutorRepository.GetAsync(x => x.TutorId.Equals(studentProfile.TutorId), true, "User");
                            studentProfiles.Add(studentProfile);
                        }                     
                    }
                }

                var result = studentProfiles.Skip(pageSize * (pageNumber - 1)).Take(pageSize);
                var totalCount = studentProfiles.Count();

                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = totalCount };

                _response.Result = _mapper.Map<List<ChildStudentProfileDTO>>(result);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Pagination = pagination;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching child student profiles.");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        //[HttpPut("changeStatus")]
        //[Authorize]
        //public async Task<ActionResult<APIResponse>> UpdateStatusStudentProfile(ChangeStatusDTO changeStatusDTO)
        //{
        //    try
        //    {
        //        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //        if (string.IsNullOrEmpty(userId))
        //        {

        //            _response.IsSuccess = false;
        //            _response.StatusCode = HttpStatusCode.Unauthorized;
        //            _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
        //            return StatusCode((int)HttpStatusCode.Unauthorized, _response);
        //        }
        //        if (changeStatusDTO == null)
        //        {
        //            _response.StatusCode = HttpStatusCode.BadRequest;
        //            _response.IsSuccess = false;
        //            _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.STATUS_CHANGE) };
        //            return BadRequest(_response);
        //        }

        //        var studentProfile = await _studentProfileRepository.GetAsync(x => x.Id == changeStatusDTO.Id, true, "InitialAndFinalAssessmentResults,ScheduleTimeSlots");

        //        if (studentProfile == null)
        //        {
        //            _logger.LogWarning("Bad Request: changeStatusDTO is null.");
        //            _response.StatusCode = HttpStatusCode.NotFound;
        //            _response.IsSuccess = false;
        //            _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.STUDENT_PROFILE) };
        //            return NotFound(_response);
        //        }

        //        if (changeStatusDTO.StatusChange == (int)StudentProfileStatus.Teaching)
        //        {
        //            if (studentProfile.CreatedDate.AddHours(24) <= DateTime.Now)
        //            {
        //                _logger.LogWarning($"Student profile with ID: {changeStatusDTO.Id} has expired. Cannot change status to Teaching.");
        //                _response.StatusCode = HttpStatusCode.BadRequest;
        //                _response.IsSuccess = false;
        //                _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.STUDENT_PROFILE_EXPIRED) };
        //                return BadRequest(_response);
        //            }
        //        }

        //        switch (changeStatusDTO.StatusChange)
        //        {
        //            case 0:
        //                studentProfile.Status = StudentProfileStatus.Stop;
        //                studentProfile.UpdatedDate = DateTime.Now;
        //                break;
        //            case 1:
        //                studentProfile.Status = StudentProfileStatus.Teaching;
        //                studentProfile.UpdatedDate = DateTime.Now;
        //                break;
        //            case 2:
        //                studentProfile.Status = StudentProfileStatus.Reject;
        //                studentProfile.UpdatedDate = DateTime.Now;
        //                break;
        //            case 3:
        //                studentProfile.Status = StudentProfileStatus.Pending;
        //                studentProfile.UpdatedDate = DateTime.Now;
        //                break;
        //        }

        //        List<InitialAssessmentResult> initialAssessmentResults = new List<InitialAssessmentResult>();
        //        foreach (var assessment in studentProfile.InitialAndFinalAssessmentResults)
        //        {
        //            initialAssessmentResults.Add(await _initialAssessmentResultRepository.GetAsync(x => x.Id == assessment.Id && x.isInitialAssessment == true, true, "Question,Option"));
        //        }
        //        studentProfile.InitialAndFinalAssessmentResults = initialAssessmentResults;
        //        if (changeStatusDTO.StatusChange == (int)StudentProfileStatus.Teaching)
        //        {
        //            var connectionId = NotificationHub.GetConnectionIdByUserId(studentProfile.TutorId);
        //            var child = await _childInfoRepository.GetAsync(x => x.Id == studentProfile.ChildId, false, "Parent", null);
        //            if (child != null && child.Parent != null)
        //            {
        //                var notfication = new Notification()
        //                {
        //                    ReceiverId = studentProfile.TutorId,
        //                    Message = _resourceService.GetString(SD.CHANGE_STATUS_STUDENT_PROFILE_TUTOR_NOTIFICATION, child?.Parent.FullName, "chấp nhận"),
        //                    UrlDetail = string.Concat(SD.URL_FE, SD.URL_FE_TUTOR_STUDENT_PROFILE_DETAIL, studentProfile.Id),
        //                    IsRead = false,
        //                    CreatedDate = DateTime.Now
        //                };
        //                var notificationResult = await _notificationRepository.CreateAsync(notfication);
        //                if (!string.IsNullOrEmpty(connectionId))
        //                {
        //                    await _hubContext.Clients.Client(connectionId).SendAsync($"Notifications-{studentProfile.TutorId}", _mapper.Map<NotificationDTO>(notificationResult));
        //                }
        //            }

        //        }
        //        else if (changeStatusDTO.StatusChange == (int)StudentProfileStatus.Reject)
        //        {
        //            var connectionId = NotificationHub.GetConnectionIdByUserId(studentProfile.TutorId);
        //            var child = await _childInfoRepository.GetAsync(x => x.Id == studentProfile.ChildId, false, "Parent", null);
        //            if (child != null && child.Parent != null)
        //            {
        //                var notfication = new Notification()
        //                {
        //                    ReceiverId = studentProfile.TutorId,
        //                    Message = _resourceService.GetString(SD.CHANGE_STATUS_STUDENT_PROFILE_TUTOR_NOTIFICATION, child?.Parent.FullName, "từ chối"),
        //                    UrlDetail = string.Concat(SD.URL_FE, SD.URL_FE_TUTOR_STUDENT_PROFILE_DETAIL, studentProfile.Id),
        //                    IsRead = false,
        //                    CreatedDate = DateTime.Now
        //                };
        //                var notificationResult = await _notificationRepository.CreateAsync(notfication);
        //                if (!string.IsNullOrEmpty(connectionId))
        //                {
        //                    await _hubContext.Clients.Client(connectionId).SendAsync($"Notifications-{studentProfile.TutorId}", _mapper.Map<NotificationDTO>(notificationResult));
        //                }
        //            }
        //        }


        //        await _studentProfileRepository.UpdateAsync(studentProfile);

        //        if (changeStatusDTO.StatusChange == (int)SD.StudentProfileStatus.Teaching)
        //        {
        //            //Generate current week schedule
        //            foreach (var timeslot in studentProfile.ScheduleTimeSlots)
        //            {
        //                timeslot.IsDeleted = false;
        //                timeslot.AppliedDate = DateTime.Today;
        //                timeslot.UpdatedDate = DateTime.Today;
        //                await _scheduleTimeSlotRepository.UpdateAsync(timeslot);
        //                DateTime today = DateTime.Now.Date;
        //                if ((int)today.DayOfWeek > timeslot.Weekday && timeslot.Weekday != 0)
        //                {
        //                    continue;
        //                }
        //                int daysUntilTargetDay = (timeslot.Weekday - (int)today.DayOfWeek + 7) % 7;

        //                DateTime targetDate = today.AddDays(daysUntilTargetDay);
        //                if (targetDate.Date.Add(timeslot.From) < DateTime.Now)
        //                {
        //                    continue;
        //                }
        //                var schedule = new Schedule()
        //                {
        //                    TutorId = studentProfile.TutorId,
        //                    AttendanceStatus = SD.AttendanceStatus.NOT_YET,
        //                    ScheduleDate = targetDate,
        //                    StudentProfileId = studentProfile.Id,
        //                    CreatedDate = DateTime.Now,
        //                    PassingStatus = SD.PassingStatus.NOT_YET,
        //                    UpdatedDate = null,
        //                    Start = timeslot.From,
        //                    End = timeslot.To,
        //                    ScheduleTimeSlotId = timeslot.Id,
        //                    Note = ""
        //                };
        //                await _scheduleRepository.CreateAsync(schedule);
        //            }
        //        }

        //        _response.Result = _mapper.Map<StudentProfileDTO>(studentProfile);
        //        _response.StatusCode = HttpStatusCode.NoContent;
        //        _response.IsSuccess = true;
        //        return Ok(_response);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"An error occurred while changing the status for student profile {changeStatusDTO.Id}.");
        //        _response.IsSuccess = false;
        //        _response.StatusCode = HttpStatusCode.InternalServerError;
        //        _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
        //        return StatusCode((int)HttpStatusCode.InternalServerError, _response);
        //    }
        //}

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetStudentProfileById(int id)
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
                if (id <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                var roles = User.FindAll(ClaimTypes.Role).Select(x => x.Value).ToList();

                var studentProfile = await _studentProfileRepository.GetAsync(x => x.Id == id, true, "InitialAndFinalAssessmentResults,ScheduleTimeSlots");

                if (studentProfile == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.STUDENT_PROFILE) };
                    return NotFound(_response);
                }

                var childInfo = await _childInfoRepository.GetAsync(x => x.Id == studentProfile.ChildId, true, "Parent");
                studentProfile.Child = childInfo;

                List<InitialAssessmentResult> initialAssessmentResults = new List<InitialAssessmentResult>();
                foreach (var assessment in studentProfile.InitialAndFinalAssessmentResults)
                {
                    initialAssessmentResults.Add(await _initialAssessmentResultRepository.GetAsync(x => x.Id == assessment.Id, true, "Question,Option"));
                }
                studentProfile.InitialAndFinalAssessmentResults = initialAssessmentResults;
                studentProfile.Tutor = await _tutorRepository.GetAsync(x => x.TutorId.Equals(studentProfile.TutorId), true, "User");

                _response.Result = roles.Contains(SD.PARENT_ROLE) ?
                    _mapper.Map<StudentProfileDetailParentDTO>(studentProfile) : _mapper.Map<StudentProfileDetailTutorDTO>(studentProfile);

                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
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

        [HttpPut("CloseTutoring")]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> CloseTutoring(CloseTutoringCreatDTO closeDTO)
        {
            try
            {

                var tutorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(tutorId))
                {

                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.UNAUTHORIZED_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Unauthorized, _response);
                }

                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                if (userRoles == null || (!userRoles.Contains(SD.TUTOR_ROLE)))
                {

                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.FORBIDDEN_MESSAGE) };
                    return StatusCode((int)HttpStatusCode.Forbidden, _response);
                }

                if (closeDTO == null || !ModelState.IsValid)
                {
                    _logger.LogWarning($"CloseTutoringCreateDTO is null");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.END_TUTORING) };
                    return BadRequest(_response);
                }
                if (closeDTO.StudentProfileId <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                var studentProfile = await _studentProfileRepository.GetAsync(x => x.Id == closeDTO.StudentProfileId, true, "InitialAndFinalAssessmentResults,ScheduleTimeSlots");

                if (studentProfile == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.STUDENT_PROFILE) };
                    return NotFound(_response);
                }

                studentProfile.InitialAndFinalAssessmentResults.AddRange(_mapper.Map<List<InitialAssessmentResult>>(closeDTO.FinalAssessmentResults));
                studentProfile.FinalCondition = closeDTO.FinalCondition;
                studentProfile.Status = StudentProfileStatus.Stop;
                studentProfile.UpdatedDate = DateTime.Now;

                studentProfile = await _studentProfileRepository.UpdateAsync(studentProfile);

                List<InitialAssessmentResult> initialAssessmentResults = new List<InitialAssessmentResult>();
                foreach (var assessment in studentProfile.InitialAndFinalAssessmentResults)
                {
                    initialAssessmentResults.Add(await _initialAssessmentResultRepository.GetAsync(x => x.Id == assessment.Id, true, "Question,Option"));
                }
                studentProfile.InitialAndFinalAssessmentResults = initialAssessmentResults;
                studentProfile.Child = await _childInfoRepository.GetAsync(x => x.Id == studentProfile.ChildId, true, "Parent");

                // Remove schedule after stop tutoring
                var schedules = await _scheduleRepository.GetAllNotPagingAsync(x => x.StudentProfileId == closeDTO.StudentProfileId);
                var filteredSchedules = schedules.list
                    .Where(x => x.ScheduleDate.Date.Add(x.Start) > DateTime.Now)
                    .ToList();
                foreach (var schedule in filteredSchedules)
                {
                    await _scheduleRepository.RemoveAsync(schedule);
                }

                // Notification
                var child = await _childInfoRepository.GetAsync(x => x.Id == studentProfile.ChildId, true, "Parent");
                var tutor = await _tutorRepository.GetAsync(x => x.TutorId.Equals(tutorId), true, "User");

                if (child != null && child.Parent != null && tutor != null && tutor.User != null)
                {
                    var connectionId = NotificationHub.GetConnectionIdByUserId(child.ParentId);
                    var notfication = new Notification()
                    {
                        ReceiverId = child.ParentId,
                        Message = _resourceService.GetString(SD.STOP_TUTORING, tutor.User.FullName, child.Name),
                        UrlDetail = string.Concat(SD.URL_FE, SD.URL_FE_PARENT_STUDENT_PROFILE_LIST, studentProfile.Id),
                        IsRead = false,
                        CreatedDate = DateTime.Now
                    };
                    var notificationResult = await _notificationRepository.CreateAsync(notfication);
                    if (!string.IsNullOrEmpty(connectionId))
                    {
                        await _hubContext.Clients.Client(connectionId).SendAsync($"Notifications-{child.ParentId}", _mapper.Map<NotificationDTO>(notificationResult));
                    }
                }

                var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "AssignedExerciseTemplate.cshtml");
                if (System.IO.File.Exists(templatePath) && child.Parent != null && tutor != null && tutor.User != null)
                {
                    var subject = "Thông Báo Kết Thúc Dạy";
                    var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);
                    var htmlMessage = templateContent
                        .Replace("@Model.ParentName", child.Parent.FullName)
                        .Replace("@Model.TutorName", tutor.User.FullName)
                        .Replace("@Model.StudentName", child.Name)
                        .Replace("@Model.FeedBackUrl", string.Concat(SD.URL_FE, SD.URL_FE_TUTOR_PROFILE, tutor.TutorId))
                        .Replace("@Model.Mail", SD.MAIL)
                        .Replace("@Model.Phone", SD.PHONE_NUMBER)
                        .Replace("@Model.WebsiteURL", SD.URL_FE);

                    await _messageBus.SendEmailAsync(child.Parent.Email, subject, htmlMessage);
                }

                _response.Result = _mapper.Map<StudentProfileDetailTutorDTO>(studentProfile);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while fetching close tutoring");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet("GetStudentProfileScheduleTimeSlot/{studentProfileId}")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetStudentProfileScheduleTimeSlot(int studentProfileId)
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
                if (studentProfileId <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                var scheduleTimeSlots = await _studentProfileRepository.GetAsync(x => x.Id == studentProfileId, true, "ScheduleTimeSlots,Child");

                if (scheduleTimeSlots == null)
                {
                    _logger.LogWarning($"No schedule time slots found for Student Profile ID: {studentProfileId}");
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.TIME_SLOT) };
                    return NotFound(_response);
                }

                _response.Result = _mapper.Map<GetAllStudentProfileTimeSlotDTO>(scheduleTimeSlots);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while fetching schedule time slots for Student Profile ID: {studentProfileId}");
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
