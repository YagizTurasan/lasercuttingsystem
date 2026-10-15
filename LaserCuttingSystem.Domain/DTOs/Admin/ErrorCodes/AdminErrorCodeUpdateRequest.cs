namespace LaserCuttingSystem.Domain.DTOs.Admin.ErrorCodes
{
    public record AdminErrorCodeUpdateRequest
    {
        public string Code { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string? Solutions { get; init; }
        public Guid? MachineId { get; init; }
        public Guid? MachinePartId { get; init; }
        public Guid? ErrorSourceId { get; init; }
        public bool IsFreeAccess { get; init; }
        public bool IsCommonAcrossAllMachines { get; init; }
        public bool IsActive { get; init; } = true;
    }
}
