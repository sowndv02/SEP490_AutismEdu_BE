using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models.DTOs.UpdateDTOs;
using backend_api.RabbitMQSender;
using backend_api.Repository.IRepository;
using backend_api.Services.IServices;
using backend_api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using static backend_api.SD;

namespace backend_api.Controllers.v1
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
        private readonly IRabbitMQMessageSender _messageBus;
        private string queueName = string.Empty;
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IBlobStorageRepository _blobStorageRepository;
        private readonly IScheduleRepository _scheduleRepository;
        private readonly IResourceService _resourceService;

        protected APIResponse _response;
        private readonly IMapper _mapper;
        protected int pageSize = 0;

        public StudentProfileController(IStudentProfileRepository studentProfileRepository, IAssessmentQuestionRepository assessmentQuestionRepository,
            IScheduleTimeSlotRepository scheduleTimeSlotRepository, IInitialAssessmentResultRepository initialAssessmentResultRepository
            , IChildInformationRepository childInfoRepository, ITutorRequestRepository tutorRequestRepository,
            IMapper mapper, IConfiguration configuration, ITutorRepository tutorRepository, IRabbitMQMessageSender messageBus,
            IUserRepository userRepository, IRoleRepository roleRepository, IBlobStorageRepository blobStorageRepository
            , IScheduleRepository scheduleRepository, IResourceService resourceService)
        {
            _studentProfileRepository = studentProfileRepository;
            _assessmentQuestionRepository = assessmentQuestionRepository;
            _scheduleTimeSlotRepository = scheduleTimeSlotRepository;
            _initialAssessmentResultRepository = initialAssessmentResultRepository;
            _childInfoRepository = childInfoRepository;
            queueName = configuration.GetValue<string>("RabbitMQSettings:QueueName");
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
        }

        [HttpPost]
        [Authorize(Roles = SD.TUTOR_ROLE)]
        public async Task<ActionResult<APIResponse>> CreateAsync([FromForm] StudentProfileCreateDTO createDTO)
        {
            try
            {
                var tutorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (createDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.STUDENT_PROFILE) };
                    return BadRequest(_response);
                }

                List<ScheduleTimeSlot> scheduleTimeSlot = _mapper.Map<List<ScheduleTimeSlot>>(createDTO.ScheduleTimeSlots);
                foreach (var slot in scheduleTimeSlot)
                {
                    var isTimeSlotDuplicate = scheduleTimeSlot.Where(x => x != slot 
                                                                       && x.Weekday == slot.Weekday 
                                                                       && !(slot.To <= x.From || slot.From >= x.To)).FirstOrDefault();
                    if (isTimeSlotDuplicate == null)
                    {
                        if(slot.From >= slot.To)
                        {
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
                                                                                           ,true, "StudentProfile");
                        if (isTimeSlotDuplicate != null)
                        {
                            _response.StatusCode = HttpStatusCode.BadRequest;
                            _response.IsSuccess = false;
                            _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.TIMESLOT_DUPLICATED_MESSAGE, SD.TIME_SLOT, isTimeSlotDuplicate.From.ToString(@"hh\:mm"), isTimeSlotDuplicate.To.ToString(@"hh\:mm")) };
                            return BadRequest(_response);
                        }
                    }
                    else
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.TIMESLOT_DUPLICATED_MESSAGE, SD.TIME_SLOT, isTimeSlotDuplicate.From.ToString(@"hh\:mm"), isTimeSlotDuplicate.To.ToString(@"hh\:mm")) };
                        return BadRequest(_response);
                    }
                }

                StudentProfile model = _mapper.Map<StudentProfile>(createDTO);
                model.CreatedDate = DateTime.Now;
                model.TutorId = tutorId;

                foreach(var assessment in model.InitialAndFinalAssessmentResults)
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
                    if(createDTO.TutorRequestId > 0)
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.STUDENT_PROFILE) };
                        return BadRequest(_response);
                    }

                    model.Status = SD.StudentProfileStatus.Teaching;
                    // Tao account parent
                    var parentEmailExist = await _userRepository.GetAsync(x => x.Email.Equals(createDTO.Email));
                    if (parentEmailExist != null)
                    {
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
                        RoleIds = new List<string>() { _roleRepository.GetByNameAsync(SD.PARENT_ROLE).GetAwaiter().GetResult().Id }
                    }, passsword);

                    // TODO: Send mail
                    var subject = "Thông báo ";

                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "CreateParentAndChild.cshtml");
                    var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);

                    var htmlMessage = templateContent
                        .Replace("@Model.FullName", parent.FullName)
                        .Replace("@Model.Username", parent.Email)
                        .Replace("@Model.Password", passsword)
                        .Replace("@Model.LoginUrl", SD.URL_FE_PARENT_LOGIN);

                    _messageBus.SendMessage(new EmailLogger()
                    {
                        Email = parent.Email,
                        Subject = subject,
                        Message = htmlMessage
                    }, queueName);

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

                    model.ChildId = childInformation.Id;
                }
                else
                {
                    model.Status = SD.StudentProfileStatus.Pending;
                }

                if (model.ChildId <= 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.MISSING_2_INFORMATIONS, SD.PARENT, SD.CHILD };
                    return BadRequest(_response);
                }

                var childTutorExist = await _studentProfileRepository.GetAsync(x => x.ChildId == createDTO.ChildId
                                                && x.TutorId.Equals(tutorId) && (x.Status == SD.StudentProfileStatus.Teaching || x.Status == SD.StudentProfileStatus.Pending));

                if (childTutorExist != null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.DATA_DUPLICATED_MESSAGE, SD.STUDENT_PROFILE) };
                    return BadRequest(_response);
                }

                var child = await _childInfoRepository.GetAsync(x => x.Id == model.ChildId, true, "Parent");
                model.Child = child;
                if (child == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.CHILD_INFO) };
                    return BadRequest(_response);
                }

                string[] names = child.Name.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                foreach (var name in names)
                {
                    model.StudentCode += name.ToUpper().ElementAt(0);
                }
                model.StudentCode += model.ChildId;
                model.Tutor = await _tutorRepository.GetAsync(x => x.TutorId.Equals(tutorId), true, "User");
                model = await _studentProfileRepository.CreateAsync(model);

                //TODO: send email
                if (createDTO.ChildId > 0)
                {
                    //var tutor = await _tutorRepository.GetAsync(x => x.TutorId.Equals(model.TutorId), true, "User");
                    var subject = "Thông Báo Xét Duyệt Hồ Sơ Học Sinh";
                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "CreateStudentProfileTemplate.cshtml");
                    var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);
                    var htmlMessage = templateContent
                        .Replace("@Model.ParentName", child.Parent.FullName)
                        .Replace("@Model.TutorName", model.Tutor.User.FullName)
                        .Replace("@Model.StudentName", child.Name)
                        .Replace("@Model.Email", child.Parent.Email)
                        .Replace("@Model.Url", SD.URL_FE_STUDENT_PROFILE_DETAIL + model.Id)
                        .Replace("@Model.ProfileCreationDate", model.CreatedDate.ToString("dd/MM/yyyy"));
                    _messageBus.SendMessage(new EmailLogger()
                    {
                        Email = child.Parent.Email,
                        Subject = subject,
                        Message = htmlMessage
                    }, queueName);
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
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? status = SD.STATUS_ALL, string? sort = SD.ORDER_DESC, int pageNumber = 1)
        {
            try
            {
                int totalCount = 0;
                List<StudentProfile> list = new();
                Expression<Func<StudentProfile, bool>> filter = u => true;
                bool isDesc = sort != null && sort == SD.ORDER_DESC;

                var tutorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

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
                    //List<InitialAssessmentResult> initialAssessments = _mapper.Map<List<InitialAssessmentResult>>(profile.InitialAssessmentResults);
                    //List<InitialAssessmentResultDTO> assessmentResults = new List<InitialAssessmentResultDTO>();
                    //foreach (var item in initialAssessments)
                    //{
                    //    assessmentResults.Add(_mapper.Map<InitialAssessmentResultDTO>(
                    //        await _initialAssessmentResultRepository.GetAsync(x => x.Id == item.Id, true, "Question,Option")));
                    //}
                    //profile.InitialAssessmentResults = assessmentResults;

                    var parent = await _childInfoRepository.GetAsync(x => x.Id == profile.ChildId, true, "Parent");
                    profile.Address = parent.Parent.Address;
                    profile.PhoneNumber = parent.Parent.PhoneNumber;

                    //profile.ScheduleTimeSlots = profile.ScheduleTimeSlots.OrderBy(x => x.Weekday).ThenBy(x => x.From).ToList();
                }

                totalCount = count;
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = totalCount };
                _response.Result = studentProfiles;
                _response.StatusCode = HttpStatusCode.OK;
                _response.Pagination = pagination;
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

        [HttpGet("GetAllScheduleTimeSlot")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllScheduleTimeSlot()
        {
            try
            {
                var tutorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var scheduleTimeSlots = await _studentProfileRepository.GetAllNotPagingAsync(x => x.TutorId.Equals(tutorId)
                && (x.Status == SD.StudentProfileStatus.Teaching || x.Status == SD.StudentProfileStatus.Teaching), "ScheduleTimeSlots,Child");

                _response.Result = _mapper.Map<List<GetAllStudentProfileTimeSlotDTO>>(scheduleTimeSlots.list);
                _response.StatusCode = HttpStatusCode.OK;
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

        [HttpGet("GetAllChildStudentProfile")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetAllChildStudentProfile([FromQuery] string? status = SD.STATUS_ALL)
        {
            try
            {
                var parentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var childs = await _childInfoRepository.GetAllNotPagingAsync(x => x.ParentId.Equals(parentId));

                if (childs.list == null)
                {
                    _response.Result = null;
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
                    var profile = await _studentProfileRepository.GetAsync(filter, true, "Child,Tutor");
                    if (profile != null)
                    {
                        profile.Tutor = await _tutorRepository.GetAsync(x => x.TutorId.Equals(profile.TutorId), true, "User");
                        studentProfiles.Add(profile);
                    }
                }

                _response.Result = _mapper.Map<List<ChildStudentProfileDTO>>(studentProfiles);
                _response.StatusCode = HttpStatusCode.OK;
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


        [HttpPut("changeStatus")]
        [Authorize]
        public async Task<IActionResult> ApproveOrRejectStudentProfile(ChangeStatusDTO changeStatusDTO)
        {
            try
            {
                if (changeStatusDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.STATUS_CHANGE) };
                    return BadRequest(_response);
                }

                var studentProfile = await _studentProfileRepository.GetAsync(x => x.Id == changeStatusDTO.Id, true, "InitialAndFinalAssessmentResults,ScheduleTimeSlots");

                if (studentProfile == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.STUDENT_PROFILE) };
                    return BadRequest(_response);
                }

                if (changeStatusDTO.StatusChange == (int)StudentProfileStatus.Teaching)
                {
                    if (studentProfile.CreatedDate.AddHours(24) <= DateTime.Now)
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.STUDENT_PROFILE_EXPIRED) };
                        return BadRequest(_response);
                    }
                }

                switch (changeStatusDTO.StatusChange)
                {
                    case 0:
                        studentProfile.Status = StudentProfileStatus.Stop;
                        studentProfile.UpdatedDate = DateTime.Now;
                        break;
                    case 1:
                        studentProfile.Status = StudentProfileStatus.Teaching;
                        studentProfile.UpdatedDate = DateTime.Now;
                        break;
                    case 2:
                        studentProfile.Status = StudentProfileStatus.Reject;
                        studentProfile.UpdatedDate = DateTime.Now;
                        break;
                    case 3:
                        studentProfile.Status = StudentProfileStatus.Pending;
                        studentProfile.UpdatedDate = DateTime.Now;
                        break;
                }

                List<InitialAssessmentResult> initialAssessmentResults = new List<InitialAssessmentResult>();
                foreach (var assessment in studentProfile.InitialAndFinalAssessmentResults)
                {
                    initialAssessmentResults.Add(await _initialAssessmentResultRepository.GetAsync(x => x.Id == assessment.Id && x.isInitialAssessment == true, true, "Question,Option"));
                }
                studentProfile.InitialAndFinalAssessmentResults = initialAssessmentResults;
                await _studentProfileRepository.UpdateAsync(studentProfile);

                if(changeStatusDTO.StatusChange == (int)SD.StudentProfileStatus.Teaching)
                {

                    //Generate current week schedule
                    foreach (var timeslot in studentProfile.ScheduleTimeSlots)
                    {
                        DateTime today = DateTime.Now;


                        if ((int)today.DayOfWeek > timeslot.Weekday && timeslot.Weekday != 0)
                        {
                            continue;
                        }

                        DayOfWeek targetDay = DayOfWeek.Monday;
                        switch (timeslot.Weekday)
                        {
                            // CN
                            case (int)DayOfWeek.Sunday:
                                targetDay = DayOfWeek.Sunday;
                                break;
                            // T2
                            case (int)DayOfWeek.Monday:
                                targetDay = DayOfWeek.Monday;
                                break;
                            case (int)DayOfWeek.Tuesday:
                                targetDay = DayOfWeek.Tuesday;
                                break;
                            case (int)DayOfWeek.Wednesday:
                                targetDay = DayOfWeek.Wednesday;
                                break;
                            case (int)DayOfWeek.Thursday:
                                targetDay = DayOfWeek.Thursday;
                                break;
                            case (int)DayOfWeek.Friday:
                                targetDay = DayOfWeek.Friday;
                                break;
                            case (int)DayOfWeek.Saturday:
                                targetDay = DayOfWeek.Saturday;
                                break;
                        }

                        // Calculate the difference between the target day and today's day
                        int daysUntilTargetDay = ((int)targetDay - (int)today.DayOfWeek) % 7;

                        // Calculate the target date
                        DateTime targetDate = today.AddDays(daysUntilTargetDay);

                        var schedule = new Schedule()
                        {
                            TutorId = studentProfile.TutorId,
                            AttendanceStatus = SD.AttendanceStatus.NOT_YET,
                            ScheduleDate = targetDate,
                            StudentProfileId = studentProfile.Id,
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

                _response.Result = _mapper.Map<StudentProfileDTO>(studentProfile);
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

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetStudentProfileById(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var roles = User.FindAll(ClaimTypes.Role).Select(x => x.Value).ToList();

                var studentProfile = await _studentProfileRepository.GetAsync(x => x.Id == id, true, "InitialAndFinalAssessmentResults,ScheduleTimeSlots");

                if (studentProfile == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.STUDENT_PROFILE) };
                    return BadRequest(_response);
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

                if (closeDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.END_TUTORING) };
                    return BadRequest(_response);
                }

                var studentProfile = await _studentProfileRepository.GetAsync(x => x.Id == closeDTO.StudentProfileId, true, "InitialAndFinalAssessmentResults,ScheduleTimeSlots");

                if (studentProfile == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.STUDENT_PROFILE) };
                    return BadRequest(_response);
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
                var scheduleToDelete = await _scheduleRepository.GetAllNotPagingAsync(x => x.StudentProfileId == closeDTO.StudentProfileId 
                                                                                        && x.ScheduleDate.Date > DateTime.Now);
                foreach(var schedule in scheduleToDelete.list)
                {
                    await _scheduleRepository.RemoveAsync(schedule);
                }

                _response.Result = _mapper.Map<StudentProfileDetailTutorDTO>(studentProfile);
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

        [HttpGet("GetStudentProfileScheduleTimeSlot/{studentProfileId}")]
        [Authorize]
        public async Task<ActionResult<APIResponse>> GetStudentProfileScheduleTimeSlot(int studentProfileId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var scheduleTimeSlots = await _studentProfileRepository.GetAsync(x => x.Id == studentProfileId, true,"ScheduleTimeSlots,Child");

                _response.Result = _mapper.Map<GetAllStudentProfileTimeSlotDTO>(scheduleTimeSlots);
                _response.StatusCode = HttpStatusCode.OK;
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
    }
}
