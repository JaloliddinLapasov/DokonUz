using AutoMapper;
using DokonUz.Models;
using DokonUz.DTOs;

namespace DokonUz.Profiles
{
    public class CustomerProfile : Profile
    {
        public CustomerProfile()
        {
            CreateMap<CustomerCreateDTO, Customer>();
            CreateMap<Customer, CustomerDTO>();
        }
    }
}