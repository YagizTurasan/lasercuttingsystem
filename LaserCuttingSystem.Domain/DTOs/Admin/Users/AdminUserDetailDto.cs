namespace LaserCuttingSystem.Domain.DTOs.Admin.Users
{
    public record AdminUserDetailDto
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string? CompanyName { get; init; }
        public bool IsActive { get; init; }
        public bool EmailConfirmed { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public AdminUserSubscriptionDto? ActiveSubscription { get; init; }
        public List<AdminUserSubscriptionDto> SubscriptionHistory { get; init; } = new();
        public List<AdminUserMachineSelectionDto> MachineSelections { get; init; } = new();
        public List<AdminPaymentHistoryDto> PaymentHistory { get; init; } = new();
        public List<string> Roles { get; init; } = new();
        public decimal TotalPayments { get; init; }
        public int TotalLogins { get; init; }
        public DateTime? LastLoginDate { get; init; }
    }

    public record AdminUserSubscriptionDto
    {
        public Guid Id { get; init; }
        public string PlanName { get; init; } = string.Empty;
        public string PlanDisplayName { get; init; } = string.Empty;
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }
        public string Status { get; init; } = string.Empty;
        public bool AutoRenew { get; init; }
        public bool IsCurrentlyActive { get; init; }
        public int DaysRemaining { get; init; }
    }

    public record AdminUserMachineSelectionDto
    {
        public Guid Id { get; init; }
        public Guid MachineId { get; init; }
        public string MachineBrand { get; init; } = string.Empty;
        public int SelectionOrder { get; init; }
        public DateTime SelectedAt { get; init; }
    }

    public record AdminPaymentHistoryDto
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
