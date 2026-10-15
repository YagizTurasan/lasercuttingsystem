namespace LaserCuttingSystem.Domain.DTOs.Admin.AdminDashboard
{
    public record RecentActivityDto
    {
        public string ActivityType { get; init; } = string.Empty;
        public string UserEmail { get; init; } = string.Empty;
        public string UserName { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public string TimeAgo { get; init; } = string.Empty;
    }
}
