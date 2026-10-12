using System;
using WatchStore.Core.Entities;

namespace WatchStore.Core.Entities
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        // קשר להזמנה
        public Order? Order { get; set; }

        // קשר למוצר שהוזמן
        public Product Product { get; set; } = null!;
    }
}
