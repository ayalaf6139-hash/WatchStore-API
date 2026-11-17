using WatchStore.Core.Enums;

namespace WatchStore.Core.DTOs
{
    public class CustomerResponse
    {
        public int Id { get; set; }

        public string PhoneNumber { get; set; } = string.Empty;

        public string? FullName { get; set; }

        public string? Email { get; set; }

        public CustomerType CustomerType { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
