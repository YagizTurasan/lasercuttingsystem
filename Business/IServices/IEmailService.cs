using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.IServices
{
    public interface IEmailService
    {
        Task<bool> SendEmailVerificationAsync(string email, string token);
        Task<bool> SendPasswordResetAsync(string email, string token);
        Task<bool> SendWelcomeEmailAsync(string email, string firstName);
        Task<bool> SendSubscriptionConfirmationAsync(string email, string planName, decimal amount);
        Task<bool> SendSubscriptionExpirationWarningAsync(string email, string firstName, int daysRemaining);
    }
}
