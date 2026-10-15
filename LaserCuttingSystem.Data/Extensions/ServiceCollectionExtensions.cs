using LaserCuttingSystem.Data.Context;
using LaserCuttingSystem.Data.IRepositories;
using LaserCuttingSystem.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace LaserCuttingSystem.Data.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDataLayer(this IServiceCollection services, IConfiguration configuration)
        {
            // DbContext
            services.AddDbContext<LaserCuttingDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(LaserCuttingDbContext).Assembly.FullName);
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorNumbersToAdd: null);
                    })
                    .EnableSensitiveDataLogging(false)
                    .EnableDetailedErrors(false));

            // Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Repositories
            services.AddScoped<ILaserMachineRepository, LaserMachineRepository>();
            services.AddScoped<IMachineModelRepository, MachineModelRepository>();
            services.AddScoped<IMachineSectionRepository, MachineSectionRepository>();
            services.AddScoped<IMachinePartRepository, MachinePartRepository>();
            services.AddScoped<IErrorSourceRepository, ErrorSourceRepository>();
            services.AddScoped<IErrorCodeRepository, ErrorCodeRepository>();
            services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
            services.AddScoped<IUserSubscriptionRepository, UserSubscriptionRepository>();
            services.AddScoped<IUserMachineSelectionRepository, UserMachineSelectionRepository>();
            services.AddScoped<IPaymentHistoryRepository, PaymentHistoryRepository>();
            services.AddScoped<IUserActivityRepository, UserActivityRepository>();
            services.AddScoped<IUserRefreshTokenRepository, UserRefreshTokenRepository>();

            return services;
        }
    }
}
