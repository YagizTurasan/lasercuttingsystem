using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.DTOs.Subscription
{
    public record PaymentHistoryDto
    {
        public Guid Id { get; init; }
        public decimal Amount { get; init; }
        public string Currency { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateTime TransactionDate { get; init; }
        public string PlanName { get; init; } = string.Empty;
        public string? PaymentProviderId { get; init; }
    }
}
