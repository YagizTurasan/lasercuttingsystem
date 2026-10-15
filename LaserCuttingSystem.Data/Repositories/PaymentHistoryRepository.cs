using LaserCuttingSystem.Data.Context;
using LaserCuttingSystem.Data.IRepositories;
using LaserCuttingSystem.Domain.Entities.Subscription;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Data.Repositories
{
    public class PaymentHistoryRepository : BaseRepository<PaymentHistory>, IPaymentHistoryRepository
    {
        public PaymentHistoryRepository(LaserCuttingDbContext context) : base(context) { }

        public async Task<IEnumerable<PaymentHistory>> GetUserPaymentHistoryAsync(Guid userId)
        {
            return await _dbSet
                .Include(p => p.Subscription)
                    .ThenInclude(s => s.Plan)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.TransactionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<PaymentHistory>> GetSuccessfulPaymentsAsync(Guid userId)
        {
            return await _dbSet
                .Include(p => p.Subscription)
                    .ThenInclude(s => s.Plan)
                .Where(p => p.UserId == userId && p.Status == Domain.Enums.PaymentStatus.Completed)
                .OrderByDescending(p => p.TransactionDate)
                .ToListAsync();
        }
    }
}
