using WatchStore.Core.Entities;

namespace WatchStore.Core.Interfaces
{
    public interface IConversationRepository
    {
        Task<Conversation?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<Conversation?> GetByIdWithDetailsAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<List<Conversation>> GetOpenAsync(
            int businessId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            Conversation conversation,
            CancellationToken cancellationToken = default);

        Task AddMessageAsync(
            ConversationMessage message,
            CancellationToken cancellationToken = default);

        Task AddStateTransitionAsync(
            ConversationStateTransition transition,
            CancellationToken cancellationToken = default);

        Task AddRequirementAsync(
            CustomerRequirement requirement,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(
            CancellationToken cancellationToken = default);
    }
}
