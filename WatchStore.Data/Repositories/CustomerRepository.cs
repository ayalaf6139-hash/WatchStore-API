using Microsoft.EntityFrameworkCore;
using WatchStore.Core.Entities;
using WatchStore.Core.Exceptions;
using WatchStore.Core.Interfaces;
using WatchStore.Data.Context;

namespace WatchStore.Data.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly WatchStoreDbContext _context;

        public CustomerRepository(WatchStoreDbContext context)
        {
            _context = context;
        }

        public async Task<Customer?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<Customer?> GetByPhoneAsync(
            int businessId,
            string normalizedPhoneNumber,
            CancellationToken cancellationToken = default)
        {
            return await _context.Customers
                .FirstOrDefaultAsync(
                    c => c.BusinessId == businessId &&
                         c.PhoneNumber == normalizedPhoneNumber,
                    cancellationToken);
        }

        public async Task AddAsync(
            Customer customer,
            CancellationToken cancellationToken = default)
        {
            await _context.Customers.AddAsync(customer, cancellationToken);
        }

        public async Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.SaveChangesWithConcurrencyCheckAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.IsUniqueViolation(DbConstraintNames.CustomerPhone))
            {
                throw new DuplicateKeyException(DbConstraintNames.CustomerPhone, ex);
            }
        }
    }
}
