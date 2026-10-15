namespace LaserCuttingSystem.Domain.DTOs.Admin.Machines
{
    public record AdminMachineListDto
    {
        public Guid Id { get; init; }
        public string Brand { get; init; } = string.Empty;
        public string? Description { get; init; }
        public int ModelsCount { get; init; }
        public int ErrorCodesCount { get; init; }
        public int UsersCount { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
