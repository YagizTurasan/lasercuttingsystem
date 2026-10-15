using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.DTOs.Payment
{
    public record PaymentRequest
    {
        public decimal Amount { get; init; }
        public string Currency { get; init; } = "TRY";
        public string PaymentMethod { get; init; } = string.Empty;
        public string? PaymentMethodId { get; init; }
        public Dictionary<string, object> Metadata { get; init; } = new();
    }
}
