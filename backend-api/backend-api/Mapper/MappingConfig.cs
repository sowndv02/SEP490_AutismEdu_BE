using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models.DTOs.UpdateDTOs;
using Microsoft.AspNetCore.Identity;

namespace backend_api.Mapper
{
    public class MappingConfig : Profile
    {
        public MappingConfig()
        {
            CreateMap<RoleDTO, IdentityRole>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ReverseMap();
            CreateMap<UserClaim, ClaimDTO>().ReverseMap();

            CreateMap<ApplicationClaim, ClaimDTO>()
                .ForMember(dest => dest.ClaimType, opt => opt.MapFrom(src => src.ClaimType))
                .ForMember(dest => dest.ClaimValue, opt => opt.MapFrom(src => src.ClaimValue))
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => src.UpdatedDate))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ReverseMap();

            CreateMap<ClaimCreateDTO, ApplicationClaim>().ReverseMap();
            CreateMap<ApplicationUser, UserCreateDTO>().ReverseMap();
            CreateMap<Tutor, TutorRegistrationRequestCreateDTO>().ReverseMap();

            CreateMap<RoleCreateDTO, IdentityRole>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ReverseMap();

            CreateMap<ApplicationUser, ApplicationUserDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.UserClaim, opt => opt.MapFrom(src => src.UserClaim))
                .ForMember(dest => dest.IsLockedOut, opt => opt.MapFrom(src => src.IsLockedOut))
                .ReverseMap();

            CreateMap<WorkExperience, WorkExperienceCreateDTO>().ReverseMap();
            CreateMap<Certificate, CertificateCreateDTO>().ReverseMap();
            CreateMap<CertificateMedia, CertificateMediaDTO>().ReverseMap();
            CreateMap<Tutor, TutorInfoDTO>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.User.PhoneNumber))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.User.ImageUrl))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.User.Address))
                .ReverseMap();

            CreateMap<Tutor, TutorDTO>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.User.Id))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
                .ForMember(dest => dest.StartAge, opt => opt.MapFrom(src => src.StartAge))
                .ForMember(dest => dest.EndAge, opt => opt.MapFrom(src => src.EndAge))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.User.Address))
                .ForMember(dest => dest.AboutMe, opt => opt.MapFrom(src => src.AboutMe))
                .ForMember(dest => dest.PriceFrom, opt => opt.MapFrom(src => src.PriceFrom))
                .ForMember(dest => dest.PriceEnd, opt => opt.MapFrom(src => src.PriceEnd))
                .ForMember(dest => dest.SessionHours, opt => opt.MapFrom(src => src.SessionHours))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.User.PhoneNumber))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.TotalReview, opt => opt.MapFrom(src => src.TotalReview))
                .ForMember(dest => dest.Certificates, opt => opt.MapFrom(src => src.Certificates.Where(x => string.IsNullOrEmpty(x.IdentityCardNumber) && !x.IsDeleted)))
                .ForMember(dest => dest.Curriculums, opt => opt.MapFrom(src => src.Curriculums.Where(x => x.IsActive && !x.IsDeleted).OrderBy(x => x.AgeFrom)))
                .ForMember(dest => dest.WorkExperiences, opt => opt.MapFrom(src => src.WorkExperiences.Where(x => x.IsActive && !x.IsDeleted).OrderBy(x => x.StartDate)))
                .ForMember(dest => dest.ReviewScore, opt => opt.MapFrom(src => src.ReviewScore == 0 ? 5 : src.ReviewScore))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.User.ImageUrl));

            CreateMap<TutorRegistrationRequest, TutorRegistrationRequestCreateDTO>().ReverseMap();
            CreateMap<TutorDTO, Tutor>();
            CreateMap<TutorProfileUpdateRequestDTO, TutorProfileUpdateRequest>().ReverseMap();

            CreateMap<Curriculum, CurriculumCreateDTO>().ReverseMap();
            CreateMap<Certificate, CertificateDTO>().ReverseMap();
            CreateMap<CertificateMedia, CertificateMediaCreateDTO>().ReverseMap();

            CreateMap<ChildInformation, ChildInformationDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.isMale ? "Male" : "Female"))
                .ForMember(dest => dest.BirthDate, opt => opt.MapFrom(src => src.BirthDate))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => src.UpdatedDate))
                .ForMember(dest => dest.ParentPhoneNumber, opt => opt.MapFrom(src => src.Parent.PhoneNumber))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Parent.Address))
                .ForMember(dest => dest.ImageUrlPath, opt => opt.MapFrom(src => src.ImageUrlPath));

            CreateMap<ChildInformation, ChildInformationCreateDTO>().ReverseMap();
            CreateMap<AvailableTimeSlot, AvailableTimeSlotCreateDTO>().ReverseMap();
            CreateMap<TutorProfileUpdateRequestCreateDTO, TutorProfileUpdateRequest>().ReverseMap();


            CreateMap<TutorRegistrationRequest, TutorRegistrationRequestDTO>().ReverseMap();
            CreateMap<WorkExperience, WorkExperienceDTO>().ReverseMap();
            CreateMap<Curriculum, CurriculumDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.AgeFrom, opt => opt.MapFrom(src => src.AgeFrom))
                .ForMember(dest => dest.AgeEnd, opt => opt.MapFrom(src => src.AgeEnd))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.RequestStatus, opt => opt.MapFrom(src => src.RequestStatus))
                .ForMember(dest => dest.RejectionReason, opt => opt.MapFrom(src => src.RejectionReason))
                .ForMember(dest => dest.ApprovedBy, opt => opt.MapFrom(src => src.ApprovedBy))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.VersionNumber, opt => opt.MapFrom(src => src.VersionNumber))
                .ForMember(dest => dest.OriginalDescription, opt => opt.MapFrom(src => src.OriginalCurriculum != null ? src.OriginalCurriculum.Description : string.Empty))
                .ForMember(dest => dest.OriginalAgeFrom, opt => opt.MapFrom(src => src.OriginalCurriculum != null ? src.OriginalCurriculum.AgeFrom : 0))
                .ForMember(dest => dest.OriginalAgeEnd, opt => opt.MapFrom(src => src.OriginalCurriculum != null ? src.OriginalCurriculum.AgeEnd : 0))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => src.UpdatedDate))
                .ReverseMap();
            CreateMap<TutorRequest, TutorRequestCreateDTO>().ReverseMap();
            CreateMap<TutorRequest, TutorRequestDTO>().ReverseMap();
            CreateMap<Blog, BlogCreateDTO>().ReverseMap();
            CreateMap<ReviewCreateDTO, Review>().ReverseMap();
            CreateMap<Review, ReviewDTO>().ReverseMap();
            CreateMap<AvailableTimeSlot, AvailableTimeSlotDTO>()
                .ForMember(dest => dest.TimeSlotId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.TimeSlot, opt => opt.MapFrom(src => $"{src.From.ToString(@"hh\:mm")}-{src.To.ToString(@"hh\:mm")}"));

            CreateMap<AssessmentQuestion, AssessmentQuestionCreateDTO>().ReverseMap();
            CreateMap<AssessmentOption, AssessmentOptionCreateDTO>().ReverseMap();

            CreateMap<AssessmentQuestion, AssessmentQuestionDTO>().ReverseMap();
            CreateMap<AssessmentOption, AssessmentOptionDTO>().ReverseMap();
            CreateMap<ChildInformation, ChildInformationUpdateDTO>().ReverseMap();
            
            CreateMap<ScheduleTimeSlot, ScheduleTimeSlotCreateDTO>().ReverseMap();
            CreateMap<InitialAssessmentResult, InitialAssessmentResultCreateDTO>().ReverseMap();

            CreateMap<InitialAssessmentResult, InitialAssessmentResultDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Question, opt => opt.MapFrom(src => src.Option.Question.Question))
                .ForMember(dest => dest.OptionText, opt => opt.MapFrom(src => src.Option.OptionText))
                .ForMember(dest => dest.Point, opt => opt.MapFrom(src => src.Option.Point))
                .ReverseMap();
            CreateMap<StudentProfile, StudentProfileDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.TutorId, opt => opt.MapFrom(src => src.TutorId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Child.Name))
                .ForMember(dest => dest.isMale, opt => opt.MapFrom(src => src.Child.isMale))
                .ForMember(dest => dest.BirthDate, opt => opt.MapFrom(src => src.Child.BirthDate))
                .ForMember(dest => dest.ImageUrlPath, opt => opt.MapFrom(src => src.Child.ImageUrlPath))
                .ForMember(dest => dest.InitialCondition, opt => opt.MapFrom(src => src.InitialCondition))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Child.Parent.Address))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.Child.Parent.PhoneNumber))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.Child.Parent.CreatedDate))
                .ForMember(dest => dest.InitialAssessmentResults, opt => opt.MapFrom(src => src.InitialAssessmentResults))
                .ForMember(dest => dest.ScheduleTimeSlots, opt => opt.MapFrom(src => src.ScheduleTimeSlots))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
                .ReverseMap();
            CreateMap<ScheduleTimeSlot, ScheduleTimeSlotDTO>().ReverseMap();
            CreateMap<StudentProfile, GetAllStudentProfileTimeSlotDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Child.Name))
                .ForMember(dest => dest.ScheduleTimeSlots, opt => opt.MapFrom(src => src.ScheduleTimeSlots))
                .ReverseMap();

            CreateMap<StudentProfile, ChildStudentProfileDTO>()
               .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
               .ForMember(dest => dest.ChildName, opt => opt.MapFrom(src => src.Child.Name))
               .ForMember(dest => dest.TutorName, opt => opt.MapFrom(src => src.Tutor.User.FullName))
               .ForMember(dest => dest.TutorPhoneNumber, opt => opt.MapFrom(src => src.Tutor.User.PhoneNumber))
               .ForMember(dest => dest.TutorImageUrl, opt => opt.MapFrom(src => src.Tutor.User.ImageUrl))
               .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
               .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
               .ReverseMap();

            CreateMap<StudentProfile, StudentProfileCreateDTO>()
                .ForMember(dest => dest.ChildId, opt => opt.MapFrom(src => src.ChildId))
                .ForMember(dest => dest.InitialCondition, opt => opt.MapFrom(src => src.InitialCondition))
                .ForMember(dest => dest.InitialAssessmentResults, opt => opt.MapFrom(src => src.InitialAssessmentResults))
                .ForMember(dest => dest.ScheduleTimeSlots, opt => opt.MapFrom(src => src.ScheduleTimeSlots))
                .ReverseMap();
            CreateMap<Exercise, ExerciseListDTO>().ReverseMap();
            CreateMap<Exercise, ExerciseDTO>().ReverseMap();
            CreateMap<ExerciseType, ExerciseTypeDTO>().ReverseMap();
            CreateMap<ExerciseCreateDTO, Exercise>().ReverseMap();
            CreateMap<ExerciseTypeCreateDTO, ExerciseType>().ReverseMap();

            CreateMap<StudentProfile, StudentProfileDetailDTO>()
               .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
               .ForMember(dest => dest.ChildId, opt => opt.MapFrom(src => src.ChildId))
               .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Child.Name))
               .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.StudentCode))
               .ForMember(dest => dest.isMale, opt => opt.MapFrom(src => src.Child.isMale))
               .ForMember(dest => dest.BirthDate, opt => opt.MapFrom(src => src.Child.BirthDate))
               .ForMember(dest => dest.InitialCondition, opt => opt.MapFrom(src => src.InitialCondition))
               .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
               .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
               .ForMember(dest => dest.Tutor, opt => opt.MapFrom(src => src.Tutor))
               .ForMember(dest => dest.InitialAssessmentResults, opt => opt.MapFrom(src => src.InitialAssessmentResults))
               .ForMember(dest => dest.ScheduleTimeSlots, opt => opt.MapFrom(src => src.ScheduleTimeSlots))
               .ReverseMap();

            CreateMap<ProgressReport, ProgressReportCreateDTO>().ReverseMap();
            CreateMap<ProgressReport, ProgressReportDTO>().ReverseMap();
            CreateMap<AssessmentResult, AssessmentResultCreateDTO>().ReverseMap();
            CreateMap<AssessmentResult, AssessmentResultDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.QuestionId, opt => opt.MapFrom(src => src.QuestionId))
                .ForMember(dest => dest.Question, opt => opt.MapFrom(src => src.Question.Question))
                .ForMember(dest => dest.OptionId, opt => opt.MapFrom(src => src.OptionId))
                .ForMember(dest => dest.SelectedOptionText, opt => opt.MapFrom(src => src.Option.OptionText))
                .ForMember(dest => dest.Point, opt => opt.MapFrom(src => src.Option.Point))
                .ReverseMap();

            CreateMap<StudentProfile, StudentProfileDetailDTO>()
               .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
               .ForMember(dest => dest.ChildId, opt => opt.MapFrom(src => src.ChildId))
               .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Child.Name))
               .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.StudentCode))
               .ForMember(dest => dest.isMale, opt => opt.MapFrom(src => src.Child.isMale))
               .ForMember(dest => dest.BirthDate, opt => opt.MapFrom(src => src.Child.BirthDate))
               .ForMember(dest => dest.ImageUrlPath, opt => opt.MapFrom(src => src.Child.ImageUrlPath))
               .ForMember(dest => dest.InitialCondition, opt => opt.MapFrom(src => src.InitialCondition))
               .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
               .ForMember(dest => dest.Tutor, opt => opt.MapFrom(src => src.Tutor))
               .ForMember(dest => dest.InitialAssessmentResults, opt => opt.MapFrom(src => src.InitialAssessmentResults))
               .ForMember(dest => dest.ScheduleTimeSlots, opt => opt.MapFrom(src => src.ScheduleTimeSlots))
               .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
               .ReverseMap();

            CreateMap<Syllabus, SyllabusCreateDTO>().ReverseMap().ForMember(dest => dest.SyllabusExercises, opt => opt.Ignore()); ;
            CreateMap<Syllabus, SyllabusDTO>().ReverseMap();
            CreateMap<SyllabusExercise, SyllabusExerciseDTO>().ReverseMap();
            CreateMap<SyllabusExercise, SyllabusExerciseCreateDTO>().ReverseMap();

            CreateMap<Schedule, ScheduleDTO>().ReverseMap();

        }
    }
}
