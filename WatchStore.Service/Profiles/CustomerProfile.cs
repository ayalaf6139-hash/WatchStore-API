using AutoMapper;
using WatchStore.Core.DTOs;
using WatchStore.Core.Entities;

namespace WatchStore.Service.Profiles
{
    public class CustomerProfile : Profile
    {
        public CustomerProfile()
        {
            CreateMap<Customer, CustomerResponse>();
        }
    }
}
