using Microsoft.EntityFrameworkCore;
using WatchStore.Core.Entities;
using WatchStore.Core.Interfaces;
using WatchStore.Data.Context;

namespace WatchStore.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly WatchStoreDbContext _context;

        public UserRepository(WatchStoreDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByUsernameAsync(
            string username,
            CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.Username == username,
                    cancellationToken);
        }

        public async Task<bool> UsernameExistsAsync(
            string username,
            CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AnyAsync(
                    u => u.Username == username,
                    cancellationToken);
        }

        public async Task AddAsync(
            User user,
            CancellationToken cancellationToken = default)
        {
            await _context.Users.AddAsync(
                user,
                cancellationToken);
        }

        public async Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
