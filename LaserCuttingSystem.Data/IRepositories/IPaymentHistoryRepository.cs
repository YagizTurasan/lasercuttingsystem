using LaserCuttingSystem.Domain.Entities.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Data.IRepositories
{
    public interface IPaymentHistoryRepository : IRepository<PaymentHistory>
    {
        Task<IEnumerable<PaymentHistory>> GetUserPaymentHistoryAsync(Guid userId);
        Task<IEnumerable<PaymentHistory>> GetSuccessfulPaymentsAsync(Guid userId);
    }
}
