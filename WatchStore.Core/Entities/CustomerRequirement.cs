using WatchStore.Core.Common;
using WatchStore.Core.Enums;

namespace WatchStore.Core.Entities
{
    public class CustomerRequirement : IAuditable
    {
        public int Id { get; set; }

        public int ConversationId { get; set; }

        public int? CustomerId { get; set; }

        public RequirementKind Kind { get; set; }

        public string Value { get; set; } = string.Empty;

        public decimal? Amount { get; set; }

        public int? ProductId { get; set; }

        public InfoCertainty Certainty { get; set; }

        public long? SourceMessageId { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? UpdatedAtUtc { get; set; }

        public string? UpdatedBy { get; set; }

        public Conversation Conversation { get; set; } = null!;

        public Customer? Customer { get; set; }

        public Product? Product { get; set; }

        public ConversationMessage? SourceMessage { get; set; }
    }
}
