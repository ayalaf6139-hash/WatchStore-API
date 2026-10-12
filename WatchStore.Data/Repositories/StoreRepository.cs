using Microsoft.EntityFrameworkCore;
using WatchStore.Core.Entities;
using WatchStore.Core.Interfaces;
using WatchStore.Data.Context;

namespace WatchStore.Data.Repositories
{
    public class StoreRepository : IStoreRepository
    {
        private readonly WatchStoreDbContext _context;

        public StoreRepository(WatchStoreDbContext context)
        {
            _context = context;
        }

        public async Task<Store?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await _context.Stores
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.Id == id,
                    cancellationToken);
        }

        public async Task<List<Store>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return await _context.Stores
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(
            Store store,
            CancellationToken cancellationToken = default)
        {
            await _context.Stores.AddAsync(
                store,
                cancellationToken);
        }

        public void Update(Store store)
        {
            _context.Stores.Update(store);
        }

        public void Delete(Store store)
        {
            _context.Stores.Remove(store);
        }

        public async Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}