using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LaserCuttingSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using LaserCuttingSystem.Domain.Entities.Identity;
using LaserCuttingSystem.Domain.Entities.Machine;
using LaserCuttingSystem.Domain.Entities.Error;
using LaserCuttingSystem.Domain.Entities.Subscription;
using LaserCuttingSystem.Domain.Entities.Analytics;
using Microsoft.AspNetCore.Identity;

namespace LaserCuttingSystem.Data.Context
{
    public class LaserCuttingDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {
        public LaserCuttingDbContext(DbContextOptions<LaserCuttingDbContext> options)
            : base(options)
        {
        }

        public DbSet<LaserMachine> LaserMachines { get; set; } = null!;
        public DbSet<MachineModel> MachineModels { get; set; } = null!;
        public DbSet<MachineSection> MachineSections { get; set; } = null!;
        public DbSet<MachinePart> MachineParts { get; set; } = null!;

        public DbSet<ErrorSource> ErrorSources { get; set; } = null!;
        public DbSet<ErrorCode> ErrorCodes { get; set; } = null!;

        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; } = null!;
        public DbSet<UserSubscription> UserSubscriptions { get; set; } = null!;
        public DbSet<UserMachineSelection> UserMachineSelections { get; set; } = null!;
        public DbSet<PaymentHistory> PaymentHistories { get; set; } = null!;

        public DbSet<UserActivity> UserActivities { get; set; } = null!;

        public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            builder.Entity<ApplicationUser>().ToTable("Users");
            builder.Entity<ApplicationRole>().ToTable("Roles");
            builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
            builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
            builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
            builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");

            builder.Entity<PaymentHistory>()
                .HasOne(p => p.User)
                .WithMany(u => u.PaymentHistories)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Update timestamps
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        break;
                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        break;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
