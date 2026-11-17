using Microsoft.EntityFrameworkCore;
using WatchStore.Core.Entities;
using WatchStore.Core.Enums;
using WatchStore.Core.Interfaces;
using WatchStore.Data.Context;

namespace WatchStore.Data.Repositories
{
    public class ConversationRepository : IConversationRepository
    {
        private readonly WatchStoreDbContext _context;

        public ConversationRepository(WatchStoreDbContext context)
        {
            _context = context;
        }

        public async Task<Conversation?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<Conversation?> GetByIdWithDetailsAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await _context.Conversations
                .AsNoTracking()
                .AsSplitQuery()
                .Include(c => c.Customer)
                .Include(c => c.Messages.OrderBy(m => m.SequenceNumber))
                .Include(c => c.StateTransitions.OrderBy(t => t.CreatedAtUtc))
                .Include(c => c.Requirements)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<List<Conversation>> GetOpenAsync(
            int businessId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Conversations
                .AsNoTracking()
                .Include(c => c.Customer)
                .Where(c => c.BusinessId == businessId &&
                            c.State != ConversationState.Completed &&
                            c.State != ConversationState.Failed)
                .OrderBy(c => c.StartedAtUtc)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(
            Conversation conversation,
            CancellationToken cancellationToken = default)
        {
            await _context.Conversations.AddAsync(conversation, cancellationToken);
        }

        public async Task AddMessageAsync(
            ConversationMessage message,
            CancellationToken cancellationToken = default)
        {
            await _context.ConversationMessages.AddAsync(message, cancellationToken);
        }

        public async Task AddStateTransitionAsync(
            ConversationStateTransition transition,
            CancellationToken cancellationToken = default)
        {
            await _context.ConversationStateTransitions.AddAsync(transition, cancellationToken);
        }

        public async Task AddRequirementAsync(
            CustomerRequirement requirement,
            CancellationToken cancellationToken = default)
        {
            await _context.CustomerRequirements.AddAsync(requirement, cancellationToken);
        }

        public async Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesWithConcurrencyCheckAsync(cancellationToken);
        }
    }
}
