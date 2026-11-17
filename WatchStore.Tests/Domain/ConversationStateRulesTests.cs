using WatchStore.Core.Domain;
using WatchStore.Core.Enums;

namespace WatchStore.Tests.Domain
{
    public class ConversationStateRulesTests
    {
        [Theory]
        [InlineData(ConversationState.AiActive, ConversationState.TransferRequested)]
        [InlineData(ConversationState.AiActive, ConversationState.HumanActive)]
        [InlineData(ConversationState.TransferRequested, ConversationState.HumanActive)]
        [InlineData(ConversationState.TransferRequested, ConversationState.AiResumed)]
        [InlineData(ConversationState.HumanActive, ConversationState.AiResumed)]
        [InlineData(ConversationState.HumanActive, ConversationState.Completed)]
        [InlineData(ConversationState.AiResumed, ConversationState.HumanActive)]
        [InlineData(ConversationState.AiResumed, ConversationState.Failed)]
        public void CanTransition_AllowedTransitions_ReturnsTrue(ConversationState from, ConversationState to)
        {
            Assert.True(ConversationStateRules.CanTransition(from, to));
        }

        [Theory]
        [InlineData(ConversationState.HumanActive, ConversationState.AiActive)]
        [InlineData(ConversationState.HumanActive, ConversationState.HumanActive)]
        [InlineData(ConversationState.Completed, ConversationState.AiActive)]
        [InlineData(ConversationState.Completed, ConversationState.HumanActive)]
        [InlineData(ConversationState.Failed, ConversationState.AiResumed)]
        [InlineData(ConversationState.AiActive, ConversationState.AiResumed)]
        public void CanTransition_ForbiddenTransitions_ReturnsFalse(ConversationState from, ConversationState to)
        {
            Assert.False(ConversationStateRules.CanTransition(from, to));
        }

        [Fact]
        public void EveryState_HasDefinedTransitions()
        {
            foreach (var state in Enum.GetValues<ConversationState>())
            {
                var reachable = Enum.GetValues<ConversationState>()
                    .Any(target => ConversationStateRules.CanTransition(state, target));

                Assert.Equal(!ConversationStateRules.IsTerminal(state), reachable);
            }
        }

        [Theory]
        [InlineData(ConversationState.AiActive, true)]
        [InlineData(ConversationState.AiResumed, true)]
        [InlineData(ConversationState.TransferRequested, false)]
        [InlineData(ConversationState.HumanActive, false)]
        [InlineData(ConversationState.Completed, false)]
        public void IsAiSpeaking_OnlyInAiStates(ConversationState state, bool expected)
        {
            Assert.Equal(expected, ConversationStateRules.IsAiSpeaking(state));
        }
    }
}
