namespace LaserCuttingSystem.Domain.DTOs.Admin.Machines
{
    public record AdminMachineCreateRequest
    {
        public string Brand { get; init; } = string.Empty;
        public string? Description { get; init; }
    }
}
