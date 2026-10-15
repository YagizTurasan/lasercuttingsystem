namespace LaserCuttingSystem.Domain.DTOs.Admin.Machines
{
    public record AdminMachineModelRequest
    {
        public string ModelCode { get; init; } = string.Empty;
        public string? Description { get; init; }
    }
}
