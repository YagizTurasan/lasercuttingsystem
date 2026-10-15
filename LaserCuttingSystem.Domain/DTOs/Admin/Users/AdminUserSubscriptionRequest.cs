namespace LaserCuttingSystem.Domain.DTOs.Admin.Users
{
    public record AdminUserSubscriptionRequest
    {
        public Guid PlanId { get; init; }
        public DateTime StartDate { get; init; }
        public bool AutoRenew { get; init; } = true;
    }
}
