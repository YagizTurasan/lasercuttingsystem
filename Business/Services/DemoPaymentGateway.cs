using LaserCuttingSystem.Business.IServices;
using LaserCuttingSystem.Domain.DTOs.Payment;
using LaserCuttingSystem.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.Services
{
    /// <summary>
    /// DEMO: Gerçek ödeme almaz; her ödemeyi başarılı sayar ve "DEMO-" önekli bir işlem numarası üretir.
    /// </summary>
    public class DemoPaymentGateway : IPaymentGateway
    {
        private readonly ILogger<DemoPaymentGateway> _logger;

        public DemoPaymentGateway(ILogger<DemoPaymentGateway> logger)
        {
            _logger = logger;
        }

        public Task<PaymentResult> ChargeAsync(PaymentRequest request)
        {
            var paymentId = $"DEMO-{Guid.NewGuid():N}";
            _logger.LogInformation("DEMO ödeme: {Amount} {Currency} ({PaymentId})", request.Amount, request.Currency, paymentId);

            return Task.FromResult(new PaymentResult
            {
                Success = true,
                PaymentId = paymentId,
                Status = PaymentStatus.Completed
            });
        }
    }
}
