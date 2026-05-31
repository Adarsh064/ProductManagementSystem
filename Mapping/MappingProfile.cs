using AutoMapper;
using ProductManagementSystem.DTOs;
using ProductManagementSystem.Models;

namespace ProductManagementSystem.Mapping
{

    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<SysUser, SysUserDto>()
            .ForMember(dest => dest.Password, opt => opt.Ignore()).ReverseMap();
            CreateMap<Product, ProductDto>().ReverseMap();
            CreateMap<Item, ItemDto>().ReverseMap();
        }


    }
}
