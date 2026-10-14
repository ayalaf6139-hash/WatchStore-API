using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatchStore.Core.Entities
{
    public class Product
    {
        public int Id { get; set; }

        public string ModelNumber { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Brand { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        public decimal WholesalePrice { get; set; }

        public decimal RecommendedRetailPrice { get; set; }

        public int StockQuantity { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        // Concurrency token - ממופה לעמודת המערכת xmin של PostgreSQL, שמשתנה בכל עדכון של השורה
        public uint RowVersion { get; set; }

        // קשר many-to-one לקטגוריה
        public Category Category { get; set; } = null!;

        // קשר many-to-many לתגיות דרך טבלת הקישור ProductTag
        public ICollection<ProductTag> ProductTags { get; set; }
            = new List<ProductTag>();
    }
}