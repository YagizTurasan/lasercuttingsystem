using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.DTOs.Subscription
{
    public record SubscriptionPlanDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public string Currency { get; init; } = string.Empty;
        public string BillingPeriod { get; init; } = string.Empty;
        public int Level { get; init; }

        public int MaxMachineSelection { get; init; }
        public bool CanAccessAllMachines { get; init; }

        public bool IsFreePlan { get; init; }
        public bool HasUnlimitedMachineSelection { get; init; }
        public string MachineSelectionText { get; init; } = string.Empty;
    }
}
