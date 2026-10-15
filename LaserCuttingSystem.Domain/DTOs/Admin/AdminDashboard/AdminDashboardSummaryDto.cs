namespace LaserCuttingSystem.Domain.DTOs.Admin.AdminDashboard
{
    public record AdminDashboardSummaryDto
    {
        public int TotalUsers { get; init; }
        public int ActiveUsers { get; init; }
        public int TotalErrorCodes { get; init; }
        public int TotalMachines { get; init; }
        public int ActiveSubscriptions { get; init; }
        public decimal MonthlyRevenue { get; init; }
        public decimal YearlyRevenue { get; init; }
        public DateTime LastUpdated { get; init; }
    }
}
