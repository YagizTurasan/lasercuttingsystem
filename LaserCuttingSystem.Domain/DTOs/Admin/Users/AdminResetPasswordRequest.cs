namespace LaserCuttingSystem.Domain.DTOs.Admin.Users
{
    public record AdminResetPasswordRequest
    {
        public string NewPassword { get; init; } = string.Empty;
    }
}
