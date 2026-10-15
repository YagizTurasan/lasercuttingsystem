using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using LaserCuttingSystem.Domain.Entities.Analytics;
using LaserCuttingSystem.Domain.Entities.Error;
using LaserCuttingSystem.Domain.Entities.Subscription;

namespace LaserCuttingSystem.Domain.Entities.Identity
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? CompanyName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
        public virtual ICollection<UserMachineSelection> UserMachineSelections { get; set; } = new List<UserMachineSelection>();
        public virtual ICollection<PaymentHistory> PaymentHistories { get; set; } = new List<PaymentHistory>();

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();

        [NotMapped]
        public UserSubscription? ActiveSubscription =>
            UserSubscriptions?
                .Where(s => s.IsActive && s.EndDate > DateTime.UtcNow && s.Status == Domain.Enums.SubscriptionStatus.Active)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefault();

        [NotMapped]
        public int SubscriptionLevel => ActiveSubscription?.Plan?.Level ?? 0;

        [NotMapped]
        public bool IsEnterprise => SubscriptionLevel >= 3;

        [NotMapped]
        public List<Guid> SelectedMachineIds => UserMachineSelections
            .Where(s => s.IsActive)
            .OrderBy(s => s.SelectionOrder)
            .Select(s => s.MachineId)
            .ToList();
    }
}
