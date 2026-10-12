using Microsoft.EntityFrameworkCore;
using WatchStore.Core.Entities;
using WatchStore.Core.Interfaces;
using WatchStore.Data.Context;

namespace WatchStore.Data.Repositories
{
    public class TagRepository : ITagRepository
    {
        private readonly WatchStoreDbContext _context;

        public TagRepository(WatchStoreDbContext context)
        {
            _context = context;
        }

        public async Task<Tag?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await _context.Tags
                .FirstOrDefaultAsync(
                    t => t.Id == id,
                    cancellationToken);
        }

        public async Task<List<Tag>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return await _context.Tags
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(
            Tag tag,
            CancellationToken cancellationToken = default)
        {
            await _context.Tags.AddAsync(
                tag,
                cancellationToken);
        }

        public void Update(Tag tag)
        {
            _context.Tags.Update(tag);
        }

        public void Delete(Tag tag)
        {
            _context.Tags.Remove(tag);
        }

        public async Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
