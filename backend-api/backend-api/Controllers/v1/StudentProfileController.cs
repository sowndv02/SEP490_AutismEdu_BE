using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models.DTOs.UpdateDTOs;
using backend_api.Repository;
using backend_api.Repository.IRepository;
using backend_api.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
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

        protected APIResponse _response;
        private readonly IMapper _mapper;
        protected int pageSize = 0;

        public StudentProfileController(IStudentProfileRepository studentProfileRepository, IAssessmentQuestionRepository assessmentQuestionRepository,
            IScheduleTimeSlotRepository scheduleTimeSlotRepository, IInitialAssessmentResultRepository initialAssessmentResultRepository
            , IChildInformationRepository childInfoRepository, ITutorRequestRepository tutorRequestRepository,
            IMapper mapper, IConfiguration configuration, ITutorRepository tutorRepository)
        {
            _studentProfileRepository = studentProfileRepository;
            _assessmentQuestionRepository = assessmentQuestionRepository;
            _scheduleTimeSlotRepository = scheduleTimeSlotRepository;
            _initialAssessmentResultRepository = initialAssessmentResultRepository;
            _childInfoRepository = childInfoRepository;
            _tutorRequestRepository = tutorRequestRepository;
            _response = new APIResponse();
            _mapper = mapper;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _tutorRepository = tutorRepository;
        }

        [HttpPost]
        public async Task<ActionResult<APIResponse>> CreateAsync([FromBody] StudentProfileCreateDTO createDTO)
        {
            try
            {
                var tutorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (createDTO == null || string.IsNullOrEmpty(tutorId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }

                var childTutorExist = await _studentProfileRepository.GetAsync(x => x.ChildId == createDTO.ChildId
                                                && x.TutorId.Equals(tutorId) && x.Status == SD.StudentProfileStatus.Teaching);

                if (childTutorExist != null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.CHILD_ALREADY_STUDING_THIS_TUTOR };
                    return BadRequest(_response);
                }

                List<ScheduleTimeSlot> scheduleTimeSlot = _mapper.Map<List<ScheduleTimeSlot>>(createDTO.ScheduleTimeSlots);

                for (int i = 0; i < scheduleTimeSlot.Count; i++)
                {
                    for (int j = i + 1; j < scheduleTimeSlot.Count; j++)
                    {
                        if (scheduleTimeSlot[i].Weekday == scheduleTimeSlot[j].Weekday &&
                            !(scheduleTimeSlot[i].To <= scheduleTimeSlot[j].From || scheduleTimeSlot[i].From >= scheduleTimeSlot[j].To))
                        {
                            _response.StatusCode = HttpStatusCode.BadRequest;
                            _response.IsSuccess = false;
                            _response.ErrorMessages = new List<string> { $"{SD.TIMESLOT_DUPLICATED} {scheduleTimeSlot[i].From.ToString(@"hh\:mm")}-{scheduleTimeSlot[i].To.ToString(@"hh\:mm")}" };
                            return BadRequest(_response);
                        }
                    }
                }

                foreach (var slot in scheduleTimeSlot)
                {
                    var existingTimeSlots = await _scheduleTimeSlotRepository.GetAllNotPagingAsync(x => x.Weekday == slot.Weekday && x.StudentProfile.TutorId.Equals(tutorId), "StudentProfile", null);
                    foreach (var existingTimeSlot in existingTimeSlots.list)
                    {
                        if (!(slot.To <= existingTimeSlot.From || slot.From >= existingTimeSlot.To))
                        {
                            _response.StatusCode = HttpStatusCode.BadRequest;
                            _response.IsSuccess = false;
                            _response.ErrorMessages = new List<string> { $"{SD.TIMESLOT_DUPLICATED} {existingTimeSlot.From.ToString(@"hh\:mm")}-{existingTimeSlot.To.ToString(@"hh\:mm")}" };
                            return BadRequest(_response);
                        }
                    }
                }

                StudentProfile model = _mapper.Map<StudentProfile>(createDTO);
                model.CreatedDate = DateTime.Now;
                model.TutorId = tutorId;
                if (createDTO.TutorRequestId <= 0)
                {
                    model.Status = SD.StudentProfileStatus.Pending;
                }
                else
                {
                    model.Status = SD.StudentProfileStatus.Teaching;

                    var tutorRequest = await _tutorRequestRepository.GetAsync(x => x.Id == createDTO.TutorRequestId);
                    if (tutorRequest == null)
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        _response.ErrorMessages = new List<string> { SD.NOT_FOUND_MESSAGE };
                        return BadRequest(_response);
                    }

                    tutorRequest.RequestStatus = SD.Status.APPROVE;
                    tutorRequest.UpdatedDate = DateTime.Now;
                    await _tutorRequestRepository.UpdateAsync(tutorRequest);
                }

                var child = await _childInfoRepository.GetAsync(x => x.Id == createDTO.ChildId);

                if (child == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.NOT_FOUND_MESSAGE };
                    return BadRequest(_response);
                }


                string[] names = child.Name.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                foreach (var name in names)
                {
                    model.StudentCode += name.ToUpper().ElementAt(0);
                }
                model.StudentCode += model.ChildId;

                await _studentProfileRepository.CreateAsync(model);

                //TODO: send email


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
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? status = SD.STATUS_ALL, string? sort = SD.ORDER_DESC, int pageNumber = 1)
        {
            try
            {
                int totalCount = 0;
                List<StudentProfile> list = new();
                Expression<Func<StudentProfile, bool>> filter = u => true;
                bool isDesc = sort != null && sort == SD.ORDER_DESC;

                var tutorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (tutorId == null)
                {
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
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
                                "Child,InitialAssessmentResults,ScheduleTimeSlots", pageSize: pageSize, pageNumber: pageNumber, x => x.CreatedDate, isDesc);

                var studentProfiles = _mapper.Map<List<StudentProfileDTO>>(result);
                foreach (var profile in studentProfiles)
                {
                    List<InitialAssessmentResult> initialAssessments = _mapper.Map<List<InitialAssessmentResult>>(profile.InitialAssessmentResults);
                    List<InitialAssessmentResultDTO> assessmentResults = new List<InitialAssessmentResultDTO>();
                    foreach (var item in initialAssessments)
                    {
                        assessmentResults.Add(_mapper.Map<InitialAssessmentResultDTO>(
                            await _initialAssessmentResultRepository.GetAsync(x => x.Id == item.Id, true, "Question,Option")));
                    }
                    profile.InitialAssessmentResults = assessmentResults;

                    var parent = await _childInfoRepository.GetAsync(x => x.Id == profile.ChildId, true, "Parent");
                    profile.Address = parent.Parent.Address;
                    profile.PhoneNumber = parent.Parent.PhoneNumber;

                    profile.ScheduleTimeSlots = profile.ScheduleTimeSlots.OrderBy(x => x.Weekday).ThenBy(x => x.From).ToList();
                }

                //TODO: add child image


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
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet("GetAllScheduleTimeSlot")]
        public async Task<ActionResult<APIResponse>> GetAllScheduleTimeSlot()
        {
            try
            {
                var tutorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(tutorId))
                {
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }

                var scheduleTimeSlots = await _studentProfileRepository.GetAllNotPagingAsync(x => x.TutorId.Equals(tutorId) && x.Status == SD.StudentProfileStatus.Teaching, "ScheduleTimeSlots,Child");

                _response.Result = _mapper.Map<List<GetAllStudentProfileTimeSlotDTO>>(scheduleTimeSlots.list);
                _response.StatusCode = HttpStatusCode.OK;
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

        [HttpGet("GetAllChildStudentProfile")]
        public async Task<ActionResult<APIResponse>> GetAllChildStudentProfile([FromQuery] string? status = SD.STATUS_ALL)
        {
            try
            {
                var parentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;


                if (string.IsNullOrEmpty(parentId))
                {
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }

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
                    filter = filter.AndAlso(x => x.ChildId.Equals(child.Id));
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
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPut("changeStatus")]
        //[Authorize(Policy = "UpdateTutorPolicy")]
        public async Task<IActionResult> ApproveOrRejectStudentProfile(ChangeStatusDTO changeStatusDTO)
        {
            try
            {
                if (changeStatusDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }

                var studentProfile = await _studentProfileRepository.GetAsync(x => x.Id == changeStatusDTO.Id, true, "InitialAssessmentResults,ScheduleTimeSlots");

                if (studentProfile == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.NOT_FOUND_MESSAGE };
                    return BadRequest(_response);
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
                foreach (var assessment in studentProfile.InitialAssessmentResults)
                {
                    initialAssessmentResults.Add(await _initialAssessmentResultRepository.GetAsync(x => x.Id == assessment.Id, true, "Question,Option"));
                }
                studentProfile.InitialAssessmentResults = initialAssessmentResults;

                _response.Result = _mapper.Map<StudentProfileDTO>(studentProfile);
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

        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse>> GetStudentProfileById(int id)
        {
            try
            {
                var studentProfile = await _studentProfileRepository.GetAsync(x => x.Id == id, true, "InitialAssessmentResults,ScheduleTimeSlots");

                if (studentProfile == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.NOT_FOUND_MESSAGE };
                    return BadRequest(_response);
                }

                var childInfo = await _childInfoRepository.GetAsync(x => x.Id == studentProfile.ChildId, true, "Parent");
                studentProfile.Child = childInfo;

                List<InitialAssessmentResult> initialAssessmentResults = new List<InitialAssessmentResult>();
                foreach (var assessment in studentProfile.InitialAssessmentResults)
                {
                    initialAssessmentResults.Add(await _initialAssessmentResultRepository.GetAsync(x => x.Id == assessment.Id, true, "Question,Option"));
                }
                studentProfile.InitialAssessmentResults = initialAssessmentResults;

                _response.Result = _mapper.Map<StudentProfileDTO>(studentProfile);
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
