using LaserCuttingSystem.Data.Context;
using LaserCuttingSystem.Data.IRepositories;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Data.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly LaserCuttingDbContext _context;
        private IDbContextTransaction? _transaction;

        // Repository instances
        private ILaserMachineRepository? _laserMachines;
        private IMachineModelRepository? _machineModels;
        private IMachineSectionRepository? _machineSections;
        private IMachinePartRepository? _machineParts;
        private IErrorSourceRepository? _errorSources;
        private IErrorCodeRepository? _errorCodes;
        private ISubscriptionPlanRepository? _subscriptionPlans;
        private IUserSubscriptionRepository? _userSubscriptions;
        private IUserMachineSelectionRepository? _userMachineSelections;
        private IPaymentHistoryRepository? _paymentHistories;
        private IUserActivityRepository? _userActivities;
        private IUserRefreshTokenRepository? _refreshTokens;

        public UnitOfWork(LaserCuttingDbContext context)
        {
            _context = context;
        }

        public LaserCuttingDbContext Context => _context;

        // Machine Repositories
        public ILaserMachineRepository LaserMachines =>
            _laserMachines ??= new LaserMachineRepository(_context);

        public IMachineModelRepository MachineModels =>
            _machineModels ??= new MachineModelRepository(_context);

        public IMachineSectionRepository MachineSections =>
            _machineSections ??= new MachineSectionRepository(_context);

        public IMachinePartRepository MachineParts =>
            _machineParts ??= new MachinePartRepository(_context);

        // Error Repositories
        public IErrorSourceRepository ErrorSources =>
            _errorSources ??= new ErrorSourceRepository(_context);

        public IErrorCodeRepository ErrorCodes =>
            _errorCodes ??= new ErrorCodeRepository(_context);

        // Subscription Repositories
        public ISubscriptionPlanRepository SubscriptionPlans =>
            _subscriptionPlans ??= new SubscriptionPlanRepository(_context);

        public IUserSubscriptionRepository UserSubscriptions =>
            _userSubscriptions ??= new UserSubscriptionRepository(_context);

        public IUserMachineSelectionRepository UserMachineSelections =>
            _userMachineSelections ??= new UserMachineSelectionRepository(_context);

        public IPaymentHistoryRepository PaymentHistories =>
            _paymentHistories ??= new PaymentHistoryRepository(_context);

        // Analytics Repositories
        public IUserActivityRepository UserActivities =>
            _userActivities ??= new UserActivityRepository(_context);

        // Refresh Token Repository
        public IUserRefreshTokenRepository RefreshTokens =>
            _refreshTokens ??= new UserRefreshTokenRepository(_context);

        // Transaction & Save
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
