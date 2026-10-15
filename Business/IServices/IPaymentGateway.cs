using LaserCuttingSystem.Domain.DTOs.Payment;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.IServices
{
    /// <summary>
    /// Ödeme sağlayıcısı soyutlaması. Gerçek bir sağlayıcı (iyzico, Stripe vb.) bağlamak için
    /// bu arayüzü uygulayan bir sınıf yazıp ServiceCollectionExtensions içinde DemoPaymentGateway yerine kaydedin.
    /// </summary>
    public interface IPaymentGateway
    {
        Task<PaymentResult> ChargeAsync(PaymentRequest request);
    }
}
