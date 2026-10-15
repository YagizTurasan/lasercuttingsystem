using LaserCuttingSystem.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaserCuttingSystem.Domain.Entities.Subscription
{
    public class SubscriptionPlan : AuditableEntity
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        public decimal Price { get; set; }

        [MaxLength(3)]
        public string Currency { get; set; } = "TRY";

        public BillingPeriod BillingPeriod { get; set; }

        public int Level { get; set; } = 1;

        public int MaxMachineSelection { get; set; } = 0;

        public bool CanAccessAllMachines { get; set; } = false;

        public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();

        [NotMapped]
        public bool IsFreePlan => Price == 0;

        [NotMapped]
        public bool HasUnlimitedMachineSelection => MaxMachineSelection == -1;

        [NotMapped]
        public string MachineSelectionText => MaxMachineSelection == -1
            ? "Sınırsız makine"
            : MaxMachineSelection == 0
                ? "Makine seçimi yok"
                : $"{MaxMachineSelection} makine";
    }
}
