using System;
using System.Collections.Generic;
using WatchStore.Core.Enums;

namespace WatchStore.Core.Entities
{
    public class Order
    {
        public int Id { get; set; }

        public int StoreId { get; set; }

        public DateTime OrderDate { get; set; }

        public OrderStatus Status { get; set; }

        public decimal TotalPrice { get; set; }

        // קשר many-to-one לחנות שביצעה את ההזמנה
        public Store Store { get; set; } = null!;

        // קשר בין הזמנה לפריטי ההזמנה
        public ICollection<OrderItem> Items { get; set; }
            = new List<OrderItem>();
    }
}
