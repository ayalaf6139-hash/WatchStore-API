using WatchStore.Core.Common;

namespace WatchStore.Core.Entities
{
    public class AgentAction : ICreationTracked
    {
        public long Id { get; set; }

        public int BusinessId { get; set; }

        public int? ConversationId { get; set; }

        public string AgentName { get; set; } = string.Empty;

        public string ToolName { get; set; } = string.Empty;

        public bool Succeeded { get; set; }

        public int DurationMs { get; set; }

        public string? ErrorCode { get; set; }

        public string? Summary { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public Business Business { get; set; } = null!;

        public Conversation? Conversation { get; set; }
    }
}
