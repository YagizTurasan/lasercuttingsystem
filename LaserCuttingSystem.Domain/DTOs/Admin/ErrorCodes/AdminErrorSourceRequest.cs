namespace LaserCuttingSystem.Domain.DTOs.Admin.ErrorCodes
{
    public record AdminErrorSourceRequest
    {
        public string SourceName { get; init; } = string.Empty;
        public string? Description { get; init; }
    }
}
