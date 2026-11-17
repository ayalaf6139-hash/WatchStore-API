using System.ComponentModel.DataAnnotations;
using WatchStore.Core.Enums;

namespace WatchStore.Core.DTOs
{
    public class UpdateCustomerRequest
    {
        [StringLength(100)]
        public string? FullName { get; set; }

        [EmailAddress]
        [StringLength(200)]
        public string? Email { get; set; }

        public CustomerType CustomerType { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }
    }
}
