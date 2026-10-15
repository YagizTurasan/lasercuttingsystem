using LaserCuttingSystem.Domain.Entities.Identity;
using LaserCuttingSystem.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace LaserCuttingSystem.Domain.Entities.Subscription
{
    public class PaymentHistory : BaseEntity
    {
        public Guid UserId { get; set; }

        public Guid SubscriptionId { get; set; }

        public decimal Amount { get; set; }

        [MaxLength(3)]
        public string Currency { get; set; } = "TRY";

        public PaymentStatus Status { get; set; }

        public DateTime TransactionDate { get; set; }

        [MaxLength(200)]
        public string? PaymentProviderId { get; set; }

        public virtual ApplicationUser User { get; set; } = null!;
        public virtual UserSubscription Subscription { get; set; } = null!;
    }
}
