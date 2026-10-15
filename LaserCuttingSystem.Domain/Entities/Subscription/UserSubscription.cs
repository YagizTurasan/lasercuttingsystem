using LaserCuttingSystem.Domain.Entities.Identity;
using LaserCuttingSystem.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaserCuttingSystem.Domain.Entities.Subscription
{
    public class UserSubscription : BaseEntity
    {
        public Guid UserId { get; set; }

        public Guid PlanId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool AutoRenew { get; set; } = true;

        public SubscriptionStatus Status { get; set; }

        public virtual ApplicationUser User { get; set; } = null!;
        public virtual SubscriptionPlan Plan { get; set; } = null!;
        public virtual ICollection<PaymentHistory> PaymentHistories { get; set; } = new List<PaymentHistory>();

        [NotMapped]
        public bool IsCurrentlyActive => IsActive && Status == SubscriptionStatus.Active && EndDate > DateTime.UtcNow;

        [NotMapped]
        public int DaysRemaining => IsCurrentlyActive ? Math.Max(0, (int)(EndDate - DateTime.UtcNow).TotalDays) : 0;

        public void Cancel()
        {
            Status = SubscriptionStatus.Cancelled;
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Renew(DateTime newEndDate)
        {
            EndDate = newEndDate;
            Status = SubscriptionStatus.Active;
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
