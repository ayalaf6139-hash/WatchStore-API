using WatchStore.Core.Enums;
using static WatchStore.Core.Enums.ConversationState;

namespace WatchStore.Core.Domain
{
    public static class ConversationStateRules
    {
        private static readonly IReadOnlyDictionary<ConversationState, ConversationState[]> AllowedTransitions =
            new Dictionary<ConversationState, ConversationState[]>
            {
                [AiActive] = new[] { TransferRequested, HumanActive, Completed, Failed },
                [TransferRequested] = new[] { HumanActive, AiResumed, Completed, Failed },
                [HumanActive] = new[] { AiResumed, Completed, Failed },
                [AiResumed] = new[] { TransferRequested, HumanActive, Completed, Failed },
                [Completed] = Array.Empty<ConversationState>(),
                [Failed] = Array.Empty<ConversationState>()
            };

        public static bool CanTransition(ConversationState from, ConversationState to)
        {
            return AllowedTransitions.TryGetValue(from, out var targets) && targets.Contains(to);
        }

        public static bool IsTerminal(ConversationState state)
        {
            return state is Completed or Failed;
        }

        public static bool IsAiSpeaking(ConversationState state)
        {
            return state is AiActive or AiResumed;
        }
    }
}
