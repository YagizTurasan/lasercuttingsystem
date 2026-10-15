namespace LaserCuttingSystem.Domain.DTOs.Admin.AdminRevenueStats
{
    public record RevenueStatsDto
    {
        public decimal CurrentMonthRevenue { get; init; }
        public decimal LastMonthRevenue { get; init; }
        public decimal CurrentYearRevenue { get; init; }
        public decimal LastYearRevenue { get; init; }
        public double MonthlyGrowthRate { get; init; }
        public double YearlyGrowthRate { get; init; }
        public List<MonthlyRevenueDto> MonthlyBreakdown { get; init; } = new();
    }

    public record MonthlyRevenueDto
    {
        public int Year { get; init; }
        public int Month { get; init; }
        public string MonthName { get; init; } = string.Empty;
        public decimal Revenue { get; init; }
        public int TransactionCount { get; init; }
    }
}
