using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace backend_api.Mapper
{
    public class MappingConfig : Profile
    {
        public MappingConfig()
        {
            CreateMap<RoleDTO, IdentityRole>().ReverseMap();

            CreateMap<ApplicationClaim, ClaimDTO>().ReverseMap();
            

            CreateMap<ApplicationUser, ApplicationUserDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.DateCreated, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.UserClaim, opt => opt.MapFrom(src => src.UserClaim))
                .ForMember(dest => dest.IsLockedOut, opt => opt.MapFrom(src => src.IsLockedOut))
                .ReverseMap();
        }
    }
}
