namespace WatchStore.Core.DTOs
{
    // גרסה מקוצרת של מוצר לתצוגת רשימה
    public class ProductListResponse
    {
        public int Id { get; set; }

        public string ModelNumber { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Brand { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;

        public decimal WholesalePrice { get; set; }

        public decimal RecommendedRetailPrice { get; set; }

        public int StockQuantity { get; set; }

        public bool IsActive { get; set; }
    }
}
