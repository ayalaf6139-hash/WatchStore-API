using AutoMapper;
using WatchStore.Core.DTOs;
using WatchStore.Core.Entities;

namespace WatchStore.Service.Profiles
{
    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            CreateMap<Order, OrderResponse>();

            CreateMap<OrderItem, OrderItemResponse>()
                .ForMember(dest => dest.ProductName,
                    opt => opt.MapFrom(src => src.Product.Name))
                .ForMember(dest => dest.TotalPrice,
                    opt => opt.MapFrom(src => src.Quantity * src.UnitPrice));
        }
    }
}
