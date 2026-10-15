namespace LaserCuttingSystem.Domain.DTOs.Admin.Machines
{
    public record AdminMachinePartRequest
    {
        public string PartName { get; init; } = string.Empty;
        public string? Description { get; init; }
    }
}
