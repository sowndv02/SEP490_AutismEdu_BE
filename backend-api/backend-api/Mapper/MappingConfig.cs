using AutoMapper;
using backend_api.Models.DTOs;
using Microsoft.AspNetCore.Identity;

namespace backend_api.Mapper
{
    public class MappingConfig : Profile
    {
        public MappingConfig()
        {
            CreateMap<RoleDTO, IdentityRole>().ReverseMap();
        }
    }
}
