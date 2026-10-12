using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;

namespace WatchStore.Core.DTOs
{
    public class CreateOrderRequest
    {
        // אין כאן StoreId: החנות נלקחת מה-token של המשתמש המחובר,
        // כדי שחנות לא תוכל להזמין בשם חנות אחרת
        [Required]
        [MinLength(1)]
        public List<CreateOrderItemRequest> Items { get; set; } = new();
    }

    public class CreateOrderItemRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}