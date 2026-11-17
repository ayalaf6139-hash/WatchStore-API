using WatchStore.Core.Common;
using WatchStore.Core.Enums;

namespace WatchStore.Core.Entities
{
    public class ConversationMessage : ICreationTracked
    {
        public long Id { get; set; }

        public int ConversationId { get; set; }

        public int SequenceNumber { get; set; }

        public MessageRole Role { get; set; }

        public string Content { get; set; } = string.Empty;

        public string? AgentName { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public Conversation Conversation { get; set; } = null!;
    }
}
