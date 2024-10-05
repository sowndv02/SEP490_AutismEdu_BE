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

            CreateMap<RoleCreateDTO, IdentityRole>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ReverseMap();
            CreateMap<Tutor, TutorCreateDTO>().ReverseMap();
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
            CreateMap<Certificate, CertificateDTO>().ReverseMap();
            CreateMap<CertificateMedia, CertificateMediaDTO>().ReverseMap();
            CreateMap<TutorInfo, Tutor>().ReverseMap();
            CreateMap<Tutor, TutorInfoDTO>().ReverseMap();

            CreateMap<Tutor, TutorDTO>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.User.PhoneNumber))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.User.Role))
                .ForMember(dest => dest.UserClaim, opt => opt.MapFrom(src => src.User.UserClaim))
                .ForMember(dest => dest.IsLockedOut, opt => opt.MapFrom(src => src.User.IsLockedOut))
                .ForMember(dest => dest.UserClaimId, opt => opt.Ignore())
                .ForMember(dest => dest.UserType, opt => opt.MapFrom(src => src.User.UserType))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.User.ImageUrl))
                .ForMember(dest => dest.ImageLocalUrl, opt => opt.MapFrom(src => src.User.ImageLocalUrl))
                .ForMember(dest => dest.ImageLocalPathUrl, opt => opt.MapFrom(src => src.User.ImageLocalPathUrl))
                .ForMember(dest => dest.Image, opt => opt.Ignore());

            CreateMap<ChildInformation, ChildInformationDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.isMale ? "Male" : "Female"))
                .ForMember(dest => dest.BirthDate, opt => opt.MapFrom(src => src.BirthDate.Value.ToString("dd/MM/yyyy")))
                .ForMember(dest => dest.InitialCondition, opt => opt.MapFrom(src => src.InitialCondition))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => src.UpdatedDate));
            CreateMap<ChildInformation, ChildInformationCreateDTO>().ReverseMap();

            CreateMap<AvailableTime, AvailableTimeCreateDTO>().ReverseMap();
            CreateMap<AvailableTimeSlot, AvailableTimeSlotCreateDTO>().ReverseMap();
            CreateMap<AvailableTimeSlot, AvailableTimeSlotDTO>()
                .ForMember(dest => dest.TimeSLot, opt => opt.MapFrom(src => $"{src.From.ToString(@"hh\:mm")}-{src.To.ToString(@"hh\:mm")}"));
            
        }
    }
}
