using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.DTOs.Subscription
{
    public record UserSubscriptionDto
    {
        public Guid Id { get; init; }
        public SubscriptionPlanDto Plan { get; init; } = null!;
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }
        public string Status { get; init; } = string.Empty;
        public bool AutoRenew { get; init; }
        public bool IsCurrentlyActive { get; init; }
        public int DaysRemaining { get; init; }
    }
}
