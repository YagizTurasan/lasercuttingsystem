namespace LaserCuttingSystem.Domain.DTOs.Admin.Machines
{
    public record AdminMachineUpdateRequest
    {
        public string Brand { get; init; } = string.Empty;
        public string? Description { get; init; }
        public bool IsActive { get; init; } = true;
    }
}
