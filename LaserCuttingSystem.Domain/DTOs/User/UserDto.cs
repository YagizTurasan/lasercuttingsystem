using LaserCuttingSystem.Domain.DTOs.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.DTOs.User
{
    public record UserDto
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string? CompanyName { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public UserSubscriptionDto? ActiveSubscription { get; init; }
        public List<string> Roles { get; init; } = new();

        public bool CanSelectMachines { get; init; }
        public int SubscriptionLevel { get; init; }
        public bool IsEnterprise { get; init; }
        public List<Guid> SelectedMachineIds { get; init; } = new();
    }
}
