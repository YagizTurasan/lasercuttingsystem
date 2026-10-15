namespace LaserCuttingSystem.Domain.DTOs.Admin.Users
{
    public record AdminCreateUserRequest
    {
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string? CompanyName { get; init; }
        public string Password { get; init; } = string.Empty;
        public bool EmailConfirmed { get; init; } = true;
        public List<string> Roles { get; init; } = new();
        public Guid? InitialPlanId { get; init; }
    }
}
