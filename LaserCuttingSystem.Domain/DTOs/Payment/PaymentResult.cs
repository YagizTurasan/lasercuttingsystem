using LaserCuttingSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.DTOs.Payment
{
    public record PaymentResult
    {
        public bool Success { get; init; }
        public string? PaymentId { get; init; }
        public string? ErrorMessage { get; init; }
        public PaymentStatus Status { get; init; }
        public string? RedirectUrl { get; init; }
    }
}
