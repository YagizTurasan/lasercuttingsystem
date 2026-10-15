namespace LaserCuttingSystem.Domain.DTOs.Admin.Users
{
    public record AdminUpdateUserRequest
    {
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string? CompanyName { get; init; }
        public bool IsActive { get; init; }
        public bool EmailConfirmed { get; init; }
        public List<string> Roles { get; init; } = new();
    }
}
