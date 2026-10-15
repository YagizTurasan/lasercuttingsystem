using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.DTOs.User
{
    public record UserListDto
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string? CompanyName { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public string? CurrentPlan { get; init; }
        public DateTime? SubscriptionEndDate { get; init; }
    }
}
