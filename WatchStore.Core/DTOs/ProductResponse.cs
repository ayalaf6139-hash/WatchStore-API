using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatchStore.Core.DTOs
{
    public class ProductResponse
    {
        public int Id { get; set; }

        public string ModelNumber { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Brand { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public List<string> Tags { get; set; } = new();

        public decimal WholesalePrice { get; set; }

        public decimal RecommendedRetailPrice { get; set; }

        public int StockQuantity { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
