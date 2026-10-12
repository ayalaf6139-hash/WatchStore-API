using AutoMapper;
using WatchStore.Core.DTOs;
using WatchStore.Core.Entities;

namespace WatchStore.Service.Profiles
{
    public class StoreProfile : Profile
    {
        public StoreProfile()
        {
            CreateMap<Store, StoreResponse>();
        }
    }
}
