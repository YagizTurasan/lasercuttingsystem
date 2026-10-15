using LaserCuttingSystem.Business.Services;
using LaserCuttingSystem.Domain.DTOs.Payment;
using LaserCuttingSystem.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LaserCuttingSystem.Tests;

public class DemoPaymentGatewayTests
{
    [Fact]
    public async Task Demo_odeme_her_zaman_basarilidir_ve_DEMO_onekli_islem_numarasi_uretir()
    {
        var gateway = new DemoPaymentGateway(NullLogger<DemoPaymentGateway>.Instance);

        var result = await gateway.ChargeAsync(new PaymentRequest { Amount = 299, Currency = "TRY" });

        Assert.True(result.Success);
        Assert.Equal(PaymentStatus.Completed, result.Status);
        Assert.StartsWith("DEMO-", result.PaymentId);
    }
}

public class EmailServiceTests
{
    private static EmailService CreateService(ListLogger<EmailService> logger, bool logWhenDisabled)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EmailSettings:IsEnabled"] = "false",
            ["EmailSettings:LogWhenDisabled"] = logWhenDisabled.ToString(),
            ["AppSettings:FrontendUrl"] = "http://localhost:3000/",
        }).Build();
        return new EmailService(config, logger);
    }

    [Fact]
    public async Task Kapaliyken_ve_log_modu_yoksa_gonderilmedi_doner()
    {
        var service = CreateService(new ListLogger<EmailService>(), logWhenDisabled: false);

        Assert.False(await service.SendPasswordResetAsync("user@test.com", "token"));
    }

    [Fact]
    public async Task Log_modunda_sifre_sifirlama_baglantisi_frontend_sayfasina_gider()
    {
        var logger = new ListLogger<EmailService>();
        var service = CreateService(logger, logWhenDisabled: true);

        var sent = await service.SendPasswordResetAsync("user@test.com", "a/b+c=");

        Assert.True(sent);
        // Token URL'de kaçışlanır; bağlantı frontend'deki /reset-password sayfasına gider
        Assert.Contains(logger.Messages,
            m => m.Contains("http://localhost:3000/reset-password?email=user%40test.com&token=a%2Fb%2Bc%3D"));
    }
}

/// <summary>Log mesajlarını test içinde okuyabilmek için basit bir logger.</summary>
public class ListLogger<T> : ILogger<T>
{
    public List<string> Messages { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
}
