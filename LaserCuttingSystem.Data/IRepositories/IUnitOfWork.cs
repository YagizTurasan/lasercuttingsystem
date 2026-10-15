using LaserCuttingSystem.Data.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Data.IRepositories
{
    public interface IUnitOfWork : IDisposable
    {
        // Machine Repositories
        ILaserMachineRepository LaserMachines { get; }
        IMachineModelRepository MachineModels { get; }
        IMachineSectionRepository MachineSections { get; }
        IMachinePartRepository MachineParts { get; }

        // Error Repositories
        IErrorSourceRepository ErrorSources { get; }
        IErrorCodeRepository ErrorCodes { get; }

        // Subscription Repositories
        ISubscriptionPlanRepository SubscriptionPlans { get; }
        IUserSubscriptionRepository UserSubscriptions { get; }
        IUserMachineSelectionRepository UserMachineSelections { get; }
        IPaymentHistoryRepository PaymentHistories { get; }

        // Analytics Repositories
        IUserActivityRepository UserActivities { get; }

        //
        IUserRefreshTokenRepository RefreshTokens { get; }

        LaserCuttingDbContext Context { get; }

        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
