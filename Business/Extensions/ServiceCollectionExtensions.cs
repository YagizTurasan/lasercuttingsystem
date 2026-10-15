using LaserCuttingSystem.Business.IServices.IDomainServices;
using LaserCuttingSystem.Business.IServices;
using LaserCuttingSystem.Business.Services.DomainService;
using LaserCuttingSystem.Business.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using LaserCuttingSystem.Business.IServices.IAdminServices;
using LaserCuttingSystem.Business.Services.AdminServices;

namespace LaserCuttingSystem.Business.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            services.AddHttpClient();

            services.AddScoped<IMachineAccessService, MachineAccessService>();
            services.AddScoped<ISubscriptionDomainService, SubscriptionDomainService>();
            services.AddScoped<IEmailService, EmailService>();
            // Gerçek ödeme sağlayıcısı bağlanınca burada değiştirilir
            services.AddScoped<IPaymentGateway, DemoPaymentGateway>();
            services.AddScoped<IErrorCodeService, ErrorCodeService>();
            services.AddScoped<IMachineService, MachineService>();
            services.AddScoped<ISubscriptionService, SubscriptionService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAdminDashboardService, AdminDashboardService>();
            services.AddScoped<IAdminErrorCodeService, AdminErrorCodeService>();
            services.AddScoped<IAdminMachineService, AdminMachineService>();
            services.AddScoped<IAdminUserService, AdminUserService>();

            

            return services;
        }
    }
}
