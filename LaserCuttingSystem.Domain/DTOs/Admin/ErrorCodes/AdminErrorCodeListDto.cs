namespace LaserCuttingSystem.Domain.DTOs.Admin.ErrorCodes
{
    public record AdminErrorCodeListDto
    {
        public Guid Id { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string MachineBrand { get; init; } = string.Empty;
        public string PartName { get; init; } = string.Empty;
        public string SourceName { get; init; } = string.Empty;
        public string AccessLevel { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
