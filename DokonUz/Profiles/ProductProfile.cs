using DokonUz.DTOs;
using DokonUz.Models;
using AutoMapper;

public class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<ProductCreateDTO, Product>()
.ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId))
.ForMember(dest => dest.Category, opt => opt.Ignore())
.ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ImageUrl));
        CreateMap<Product, ProductDTO>();
        CreateMap<Category, CategoryDTO>();
    }
}