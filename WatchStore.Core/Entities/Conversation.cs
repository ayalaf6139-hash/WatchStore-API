using WatchStore.Core.Common;
using WatchStore.Core.Enums;

namespace WatchStore.Core.Entities
{
    public class Conversation : IAuditable
    {
        public int Id { get; set; }

        public int BusinessId { get; set; }

        public int? CustomerId { get; set; }

        public string CallerPhoneNumber { get; set; } = string.Empty;

        public ConversationChannel Channel { get; set; }

        public ConversationState State { get; set; }

        public DateTime StartedAtUtc { get; set; }

        public DateTime? EndedAtUtc { get; set; }

        public int MessageCount { get; set; }

        public string? Summary { get; set; }

        public int? HandledByUserId { get; set; }

        public uint RowVersion { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? UpdatedAtUtc { get; set; }

        public string? UpdatedBy { get; set; }

        public Business Business { get; set; } = null!;

        public Customer? Customer { get; set; }

        public User? HandledByUser { get; set; }

        public ICollection<ConversationMessage> Messages { get; set; }
            = new List<ConversationMessage>();

        public ICollection<ConversationStateTransition> StateTransitions { get; set; }
            = new List<ConversationStateTransition>();

        public ICollection<CustomerRequirement> Requirements { get; set; }
            = new List<CustomerRequirement>();

        public ICollection<Appointment> Appointments { get; set; }
            = new List<Appointment>();
    }
}
