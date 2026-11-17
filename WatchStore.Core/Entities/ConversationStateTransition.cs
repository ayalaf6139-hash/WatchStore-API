using WatchStore.Core.Common;
using WatchStore.Core.Enums;

namespace WatchStore.Core.Entities
{
    public class ConversationStateTransition : ICreationTracked
    {
        public long Id { get; set; }

        public int ConversationId { get; set; }

        public ConversationState FromState { get; set; }

        public ConversationState ToState { get; set; }

        public string? Reason { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public Conversation Conversation { get; set; } = null!;
    }
}
