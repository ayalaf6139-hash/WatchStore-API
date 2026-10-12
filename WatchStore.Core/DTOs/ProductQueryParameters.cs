using System.ComponentModel.DataAnnotations;

namespace WatchStore.Core.DTOs
{
    // פרמטרים של GET /api/products - מגיעים מה-query string
    public class ProductQueryParameters
    {
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 50)]
        public int PageSize { get; set; } = 10;

        // חיפוש חופשי בשם, מספר דגם או מותג
        [StringLength(100)]
        public string? Search { get; set; }

        [StringLength(50)]
        public string? Brand { get; set; }

        public int? CategoryId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MinPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MaxPrice { get; set; }

        [RegularExpression("(?i)^(name|price|stock|createdAt)$",
            ErrorMessage = "sortBy must be one of: name, price, stock, createdAt.")]
        public string? SortBy { get; set; }

        [RegularExpression("(?i)^(asc|desc)$",
            ErrorMessage = "sortDirection must be asc or desc.")]
        public string SortDirection { get; set; } = "asc";
    }
}
