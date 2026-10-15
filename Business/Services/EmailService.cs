using LaserCuttingSystem.Business.IServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly bool _enableSsl;
        private readonly bool _isEnabled;
        private readonly bool _logWhenDisabled;
        private readonly string _frontendUrl;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Configuration'dan email ayarlarını al
            var emailConfig = _configuration.GetSection("EmailSettings");

            _smtpHost = emailConfig["SmtpHost"] ?? "localhost";
            _smtpPort = int.TryParse(emailConfig["SmtpPort"], out var port) ? port : 587;
            _smtpUsername = emailConfig["SmtpUsername"] ?? "";
            _smtpPassword = emailConfig["SmtpPassword"] ?? "";
            _fromEmail = emailConfig["FromEmail"] ?? "noreply@example.com";
            _fromName = emailConfig["FromName"] ?? "Laser Cutting System";
            _enableSsl = bool.TryParse(emailConfig["EnableSsl"], out var ssl) ? ssl : true;
            _isEnabled = bool.TryParse(emailConfig["IsEnabled"], out var enabled) ? enabled : false;
            _logWhenDisabled = bool.TryParse(emailConfig["LogWhenDisabled"], out var logWhenDisabled) && logWhenDisabled;
            _frontendUrl = (_configuration["AppSettings:FrontendUrl"] ?? "http://localhost:3000").TrimEnd('/');
        }

        public async Task<bool> SendEmailVerificationAsync(string email, string token)
        {
            try
            {
                var subject = "E-posta Adresinizi Doğrulayın - Laser Cutting System";

                var body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                            <h2 style='color: #2c3e50; text-align: center;'>E-posta Doğrulama</h2>
                            <p>Merhaba,</p>
                            <p>Laser Cutting System'e kayıt olduğunuz için teşekkür ederiz. E-posta adresinizi doğrulamak için aşağıdaki bağlantıya tıklayın:</p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{GetVerificationLink(email, token)}' 
                                   style='background-color: #3498db; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                                   E-posta Adresimi Doğrula
                                </a>
                            </div>
                            <p>Bu bağlantı 24 saat içinde geçersiz hale gelecektir.</p>
                            <p>Eğer bu işlemi siz yapmadıysanız, bu e-postayı görmezden gelebilirsiniz.</p>
                            <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                            <p style='font-size: 12px; color: #666; text-align: center;'>
                                Bu e-posta Laser Cutting System tarafından otomatik olarak gönderilmiştir.
                            </p>
                        </div>
                    </body>
                    </html>";

                return await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email verification gönderilirken hata oluştu: {Email}", email);
                return false;
            }
        }

        public async Task<bool> SendPasswordResetAsync(string email, string token)
        {
            try
            {
                var subject = "Şifre Sıfırlama - Laser Cutting System";

                var body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                            <h2 style='color: #2c3e50; text-align: center;'>Şifre Sıfırlama</h2>
                            <p>Merhaba,</p>
                            <p>Hesabınız için şifre sıfırlama talebi aldık. Şifrenizi sıfırlamak için aşağıdaki bağlantıya tıklayın:</p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{GetPasswordResetLink(email, token)}' 
                                   style='background-color: #e74c3c; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                                   Şifremi Sıfırla
                                </a>
                            </div>
                            <p>Bu bağlantı 1 saat içinde geçersiz hale gelecektir.</p>
                            <p>Eğer bu işlemi siz yapmadıysanız, hesabınızın güvenliği için şifrenizi değiştirmenizi öneririz.</p>
                            <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                            <p style='font-size: 12px; color: #666; text-align: center;'>
                                Bu e-posta Laser Cutting System tarafından otomatik olarak gönderilmiştir.
                            </p>
                        </div>
                    </body>
                    </html>";

                return await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password reset e-postası gönderilirken hata oluştu: {Email}", email);
                return false;
            }
        }

        public async Task<bool> SendWelcomeEmailAsync(string email, string firstName)
        {
            try
            {
                var subject = "Hoş Geldiniz - Laser Cutting System";

                var body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                            <h2 style='color: #2c3e50; text-align: center;'>Hoş Geldiniz!</h2>
                            <p>Merhaba {firstName},</p>
                            <p>Laser Cutting System ailesine katıldığınız için teşekkür ederiz! Hesabınız başarıyla oluşturuldu.</p>
                            
                            <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                                <h3 style='color: #2c3e50; margin-top: 0;'>Neler Yapabilirsiniz:</h3>
                                <ul style='color: #555;'>
                                    <li>Binlerce lazer makine hata koduna erişim</li>
                                    <li>Makine seçimi yaparak özel hata kodları görüntüleme</li>
                                    <li>Detaylı çözüm önerileri</li>
                                    <li>Premium abonelik ile sınırsız erişim</li>
                                </ul>
                            </div>

                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{GetDashboardLink()}' 
                                   style='background-color: #27ae60; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                                   Hemen Başlayın
                                </a>
                            </div>

                            <p>Herhangi bir sorunuz varsa, bizimle iletişime geçmekten çekinmeyin.</p>
                            <p>İyi çalışmalar dileriz!</p>
                            
                            <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                            <p style='font-size: 12px; color: #666; text-align: center;'>
                                Bu e-posta Laser Cutting System tarafından otomatik olarak gönderilmiştir.
                            </p>
                        </div>
                    </body>
                    </html>";

                return await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Welcome e-postası gönderilirken hata oluştu: {Email}", email);
                return false;
            }
        }

        public async Task<bool> SendSubscriptionConfirmationAsync(string email, string planName, decimal amount)
        {
            try
            {
                var subject = $"Abonelik Onayı - {planName}";

                var body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                            <h2 style='color: #2c3e50; text-align: center;'>Abonelik Onayı</h2>
                            <p>Merhaba,</p>
                            <p><strong>{planName}</strong> planına başarıyla abone oldunuz!</p>
                            
                            <div style='background-color: #e8f5e8; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #27ae60;'>
                                <h3 style='color: #27ae60; margin-top: 0;'>Abonelik Detayları</h3>
                                <p style='margin: 5px 0;'><strong>Plan:</strong> {planName}</p>
                                <p style='margin: 5px 0;'><strong>Ödeme:</strong> {amount:C} TL</p>
                                <p style='margin: 5px 0;'><strong>Tarih:</strong> {DateTime.UtcNow:dd.MM.yyyy}</p>
                            </div>

                            <p>Artık tüm premium özelliklere erişiminiz var. Hesabınıza giriş yaparak yeni özelliklerinizi keşfedebilirsiniz.</p>

                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{GetDashboardLink()}' 
                                   style='background-color: #3498db; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                                   Hesabıma Git
                                </a>
                            </div>

                            <p>Herhangi bir sorunuz varsa, destek ekibimizle iletişime geçebilirsiniz.</p>
                            
                            <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                            <p style='font-size: 12px; color: #666; text-align: center;'>
                                Bu e-posta Laser Cutting System tarafından otomatik olarak gönderilmiştir.
                            </p>
                        </div>
                    </body>
                    </html>";

                return await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscription confirmation e-postası gönderilirken hata oluştu: {Email}", email);
                return false;
            }
        }

        public async Task<bool> SendSubscriptionExpirationWarningAsync(string email, string firstName, int daysRemaining)
        {
            try
            {
                var subject = $"Aboneliğiniz {daysRemaining} Gün İçinde Sona Eriyor";

                var body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                            <h2 style='color: #e74c3c; text-align: center;'>Abonelik Uyarısı</h2>
                            <p>Merhaba {firstName},</p>
                            <p>Aboneliğinizin <strong>{daysRemaining} gün</strong> içinde sona ereceğini hatırlatmak isteriz.</p>
                            
                            <div style='background-color: #fdf2e7; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #e67e22;'>
                                <h3 style='color: #e67e22; margin-top: 0;'>Önemli Bilgi</h3>
                                <p>Aboneliğiniz sona erdiğinde:</p>
                                <ul>
                                    <li>Premium hata kodlarına erişiminiz kısıtlanacak</li>
                                    <li>Makine seçimleriniz pasif hale gelecek</li>
                                    <li>Sadece ücretsiz içeriklere erişebileceksiniz</li>
                                </ul>
                            </div>

                            <p>Kesintisiz hizmet almak için aboneliğinizi yenileyebilir veya farklı bir plana geçiş yapabilirsiniz.</p>

                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{GetSubscriptionLink()}' 
                                   style='background-color: #27ae60; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 0 10px;'>
                                   Aboneliği Yenile
                                </a>
                                <a href='{GetPlansLink()}' 
                                   style='background-color: #3498db; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 0 10px;'>
                                   Planları İncele
                                </a>
                            </div>
                            
                            <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                            <p style='font-size: 12px; color: #666; text-align: center;'>
                                Bu e-posta Laser Cutting System tarafından otomatik olarak gönderilmiştir.
                            </p>
                        </div>
                    </body>
                    </html>";

                return await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscription expiration warning e-postası gönderilirken hata oluştu: {Email}", email);
                return false;
            }
        }

        // Private helper methods
        private async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            if (!_isEnabled)
            {
                if (_logWhenDisabled)
                {
                    // Geliştirme kolaylığı: SMTP olmadan da şifre sıfırlama / doğrulama bağlantıları kullanılabilsin.
                    // Bağlantılar token içerdiği için bu mod canlı ortamda açılmamalıdır.
                    var links = Regex.Matches(body, "href='([^']+)'").Select(m => m.Groups[1].Value);
                    _logger.LogWarning(
                        "E-posta gönderimi kapalı, içerik loga yazıldı. Alıcı: {Email} | Konu: {Subject} | Bağlantılar: {Links}",
                        toEmail, subject, string.Join(" ", links));
                    return true;
                }

                _logger.LogWarning("Email servisi devre dışı. Email gönderilmedi: {Email}", toEmail);
                return false;
            }

            try
            {
                var message = new MimeKit.MimeMessage();
                message.From.Add(new MimeKit.MailboxAddress(_fromName, _fromEmail));
                message.To.Add(MimeKit.MailboxAddress.Parse(toEmail));
                message.Subject = subject;

                var bodyBuilder = new MimeKit.BodyBuilder
                {
                    HtmlBody = body,
                    TextBody = "Bu e-posta HTML formatında görüntülenemiyorsa, lütfen e-posta istemcinizi güncelleyin."
                };

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new MailKit.Net.Smtp.SmtpClient();
                // Auto: 465 portunda SSL ile bağlanır, 587 gibi portlarda STARTTLS kullanır
                await client.ConnectAsync(_smtpHost, _smtpPort,
                    _enableSsl ? MailKit.Security.SecureSocketOptions.Auto : MailKit.Security.SecureSocketOptions.None);

                // Eğer SMTP sunucusu kimlik doğrulaması gerektiriyorsa:
                if (!string.IsNullOrEmpty(_smtpUsername) && !string.IsNullOrEmpty(_smtpPassword))
                {
                    await client.AuthenticateAsync(_smtpUsername, _smtpPassword);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email başarıyla gönderildi: {Email}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MailKit ile email gönderilirken hata oluştu: {Email}", toEmail);
                return false;
            }
        }

        // Bağlantılar frontend'deki sayfalara gider (AppSettings:FrontendUrl)
        private string GetVerificationLink(string email, string token)
            => $"{_frontendUrl}/verify-email?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

        private string GetPasswordResetLink(string email, string token)
            => $"{_frontendUrl}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

        private string GetDashboardLink() => $"{_frontendUrl}/dashboard";

        private string GetSubscriptionLink() => $"{_frontendUrl}/subscription/current";

        private string GetPlansLink() => $"{_frontendUrl}/subscription";
    }
}