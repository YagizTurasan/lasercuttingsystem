namespace LaserCuttingSystem.Domain.DTOs.Admin.Machines
{
    public record AdminMachineSectionRequest
    {
        public string SectionName { get; init; } = string.Empty;
        public string? Description { get; init; }
    }
}
