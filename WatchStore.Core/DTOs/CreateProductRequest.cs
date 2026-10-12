using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;

namespace WatchStore.Core.DTOs
{
    public class CreateProductRequest
    {
        [Required]
        [StringLength(50)]
        public string ModelNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Brand { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal WholesalePrice { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal RecommendedRetailPrice { get; set; }

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }
    }
}
