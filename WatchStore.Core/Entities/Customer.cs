using WatchStore.Core.Common;
using WatchStore.Core.Enums;

namespace WatchStore.Core.Entities
{
    public class Customer : IAuditable
    {
        public int Id { get; set; }

        public int BusinessId { get; set; }

        public string PhoneNumber { get; set; } = string.Empty;

        public string? FullName { get; set; }

        public string? Email { get; set; }

        public CustomerType CustomerType { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? UpdatedAtUtc { get; set; }

        public string? UpdatedBy { get; set; }

        public Business Business { get; set; } = null!;

        public ICollection<Conversation> Conversations { get; set; }
            = new List<Conversation>();

        public ICollection<Appointment> Appointments { get; set; }
            = new List<Appointment>();
    }
}
