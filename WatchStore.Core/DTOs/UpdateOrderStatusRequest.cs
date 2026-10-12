using System.ComponentModel.DataAnnotations;
using WatchStore.Core.Enums;

namespace WatchStore.Core.DTOs
{
    public class UpdateOrderStatusRequest
    {
        [Required]
        [EnumDataType(typeof(OrderStatus))]
        public OrderStatus Status { get; set; }
    }
}
