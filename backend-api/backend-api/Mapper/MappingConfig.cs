using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
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
            CreateMap<Tutor, TutorInfoDTO>().ReverseMap();

            CreateMap<Tutor, TutorDTO>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.User.Id))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
                .ForMember(dest => dest.StartAge, opt => opt.MapFrom(src => src.StartAge))
                .ForMember(dest => dest.EndAge, opt => opt.MapFrom(src => src.EndAge))
                .ForMember(dest => dest.AboutMe, opt => opt.MapFrom(src => src.AboutMe))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.User.PhoneNumber))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.TotalReview, opt => opt.MapFrom(src => src.Reviews.Count))
                .ForMember(dest => dest.Certificates, opt => opt.MapFrom(src => src.Certificates.Where(x => string.IsNullOrEmpty(x.IdentityCardNumber) && x.IsActive)))
                .ForMember(dest => dest.Curriculums, opt => opt.MapFrom(src => src.Curriculums.Where(x => x.IsActive)))
                .ForMember(dest => dest.WorkExperiences, opt => opt.MapFrom(src => src.WorkExperiences.Where(x => x.IsActive)))
                .ForMember(dest => dest.ReviewScore, opt => opt.MapFrom(src => src.Reviews.Count > 0 ? src.Reviews.Average(x => x.RateScore) : 5))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.User.ImageUrl));

            CreateMap<TutorRegistrationRequest, TutorRegistrationRequestCreateDTO>().ReverseMap();
            CreateMap<TutorDTO, Tutor>();

            CreateMap<Curriculum, CurriculumCreateDTO>().ReverseMap();
            CreateMap<Certificate, CertificateDTO>().ReverseMap();
            CreateMap<CertificateMedia, CertificateMediaCreateDTO>().ReverseMap();

            CreateMap<ChildInformation, ChildInformationDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.isMale ? "Male" : "Female"))
                .ForMember(dest => dest.BirthDate, opt => opt.MapFrom(src => src.BirthDate.Value.ToString("dd/MM/yyyy")))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => src.UpdatedDate));

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
                .ForMember(dest => dest.OrifinalDescription, opt => opt.MapFrom(src => src.OriginalCurriculum != null ? src.OriginalCurriculum.Description : string.Empty))
                .ForMember(dest => dest.OrifinalAgeFrom, opt => opt.MapFrom(src => src.OriginalCurriculum != null ? src.OriginalCurriculum.AgeFrom : 0))
                .ForMember(dest => dest.OrifinalAgeEnd, opt => opt.MapFrom(src => src.OriginalCurriculum != null ? src.OriginalCurriculum.AgeEnd : 0))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => src.UpdatedDate))
                .ReverseMap();
            CreateMap<TutorRequest, TutorRequestCreateDTO>().ReverseMap();
            CreateMap<TutorRequest, TutorRequestDTO>().ReverseMap();
            CreateMap<Blog, BlogCreateDTO>().ReverseMap();

            CreateMap<AvailableTimeSlot, AvailableTimeSlotDTO>()
                .ForMember(dest => dest.TimeSlotId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.TimeSlot, opt => opt.MapFrom(src => $"{src.From.ToString(@"hh\:mm")}-{src.To.ToString(@"hh\:mm")}"));

            CreateMap<AssessmentQuestion, AssessmentQuestionCreateDTO>().ReverseMap();
            CreateMap<AssessmentOption, AssessmentOptionCreateDTO>().ReverseMap();
           
            CreateMap<AssessmentQuestion, AssessmentQuestionDTO>()
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate.ToString("dd/MM/yyyy")))
                .ReverseMap();
            CreateMap<AssessmentOption, AssessmentOptionDTO>().ReverseMap();
        }
    }
}
