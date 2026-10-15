namespace LaserCuttingSystem.Domain.DTOs.Admin.Users
{
    public record AdminUserListDto
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string? CompanyName { get; init; }
        public bool IsActive { get; init; }
        public bool EmailConfirmed { get; init; }
        public DateTime CreatedAt { get; init; }
        public string CurrentPlan { get; init; } = string.Empty;
        public DateTime? SubscriptionEndDate { get; init; }
        public decimal TotalPayments { get; init; }
        public int SelectedMachinesCount { get; init; }
        public List<string> Roles { get; init; } = new();
    }
}
