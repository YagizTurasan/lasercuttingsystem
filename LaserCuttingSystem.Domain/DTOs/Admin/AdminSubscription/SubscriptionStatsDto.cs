namespace LaserCuttingSystem.Domain.DTOs.Admin.AdminSubscription
{
    public record SubscriptionStatsDto
    {
        public List<PlanDistributionDto> PlanDistributions { get; init; } = new();
        public int TotalSubscriptions { get; init; }
        public decimal AverageMonthlyRevenue { get; init; }
        public decimal TotalRevenue { get; init; }
    }

    public record PlanDistributionDto
    {
        public string PlanName { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public int UserCount { get; init; }
        public decimal Revenue { get; init; }
        public double Percentage { get; init; }
    }
}
