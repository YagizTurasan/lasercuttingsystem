using LaserCuttingSystem.Data.Context;
using LaserCuttingSystem.Domain.Entities.Identity;
using LaserCuttingSystem.Domain.Entities.Subscription;
using LaserCuttingSystem.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Data.Seeders
{
    /// <summary>
    /// Uygulama açılışında rolleri, abonelik planlarını ve ilk admin kullanıcısını oluşturur. Tekrar çalıştırmak güvenlidir.
    /// </summary>
    public class InitialDataSeeder
    {
        private readonly LaserCuttingDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<InitialDataSeeder> _logger;

        public InitialDataSeeder(
            LaserCuttingDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IConfiguration configuration,
            ILogger<InitialDataSeeder> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            await SeedRolesAsync();
            await SeedSubscriptionPlansAsync();
            await SeedAdminUserAsync();
        }

        private async Task SeedRolesAsync()
        {
            var roles = new Dictionary<string, string>
            {
                ["Admin"] = "Sistem yöneticisi",
                ["Moderator"] = "İçerik moderatörü",
                ["User"] = "Normal kullanıcı",
            };

            foreach (var (roleName, description) in roles)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new ApplicationRole(roleName) { Description = description });
                }
            }
        }

        private async Task SeedSubscriptionPlansAsync()
        {
            if (!await _context.SubscriptionPlans.AnyAsync())
            {
                var plans = new List<SubscriptionPlan>
                {
                    new SubscriptionPlan
                    {
                        Name = "Free",
                        DisplayName = "Ücretsiz",
                        Price = 0,
                        Currency = "TRY",
                        BillingPeriod = BillingPeriod.Monthly,
                        Level = 0,
                        MaxMachineSelection = 0,
                        CanAccessAllMachines = false
                    },
                    new SubscriptionPlan
                    {
                        Name = "Basic",
                        DisplayName = "Temel",
                        Price = 299,
                        Currency = "TRY",
                        BillingPeriod = BillingPeriod.Monthly,
                        Level = 1,
                        MaxMachineSelection = 1,
                        CanAccessAllMachines = false
                    },
                    new SubscriptionPlan
                    {
                        Name = "Professional",
                        DisplayName = "Profesyonel",
                        Price = 799,
                        Currency = "TRY",
                        BillingPeriod = BillingPeriod.Monthly,
                        Level = 2,
                        MaxMachineSelection = 3,
                        CanAccessAllMachines = false
                    },
                    new SubscriptionPlan
                    {
                        Name = "Enterprise",
                        DisplayName = "Kurumsal",
                        Price = 2999,
                        Currency = "TRY",
                        BillingPeriod = BillingPeriod.Monthly,
                        Level = 3,
                        MaxMachineSelection = -1,
                        CanAccessAllMachines = true
                    }
                };

                await _context.SubscriptionPlans.AddRangeAsync(plans);
                await _context.SaveChangesAsync();
            }
        }

        // Admin bilgileri koda gömülü değildir; SeedAdmin:Email ve SeedAdmin:Password yapılandırmasından okunur
        private async Task SeedAdminUserAsync()
        {
            var adminEmail = _configuration["SeedAdmin:Email"];
            var adminPassword = _configuration["SeedAdmin:Password"];

            if (string.IsNullOrWhiteSpace(adminEmail))
            {
                return;
            }

            var adminUser = await _userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                if (string.IsNullOrWhiteSpace(adminPassword))
                {
                    _logger.LogWarning(
                        "Admin kullanıcısı oluşturulmadı: SeedAdmin:Password ayarlanmamış. " +
                        "User Secrets veya SeedAdmin__Password ortam değişkeni ile ayarlayın.");
                    return;
                }

                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "User",
                    CompanyName = "LaserCutting System",
                    IsActive = true,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, "Admin");
                    _logger.LogInformation("Admin kullanıcısı oluşturuldu: {Email}", adminEmail);
                }
                else
                {
                    _logger.LogError("Admin kullanıcısı oluşturulamadı: {Errors}",
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}
