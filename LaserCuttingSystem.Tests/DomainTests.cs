using LaserCuttingSystem.Domain.Entities.Error;
using LaserCuttingSystem.Domain.Entities.Subscription;

namespace LaserCuttingSystem.Tests;

public class ErrorCodeTests
{
    private static readonly Guid MachineA = Guid.NewGuid();
    private static readonly Guid MachineB = Guid.NewGuid();

    [Fact]
    public void Kurumsal_kullanici_her_koda_erisir()
    {
        var code = new ErrorCode { MachineId = MachineB };

        Assert.True(code.IsAccessibleToUser(3, new List<Guid>(), isEnterprise: true));
    }

    [Fact]
    public void Ucretsiz_koda_herkes_erisir()
    {
        var code = new ErrorCode { IsFreeAccess = true, MachineId = MachineB };

        Assert.True(code.IsAccessibleToUser(0, new List<Guid>(), isEnterprise: false));
    }

    [Fact]
    public void Premium_koda_yalnizca_secilen_makinede_erisilir()
    {
        var code = new ErrorCode { MachineId = MachineA };

        Assert.True(code.IsAccessibleToUser(1, new List<Guid> { MachineA }, isEnterprise: false));
        Assert.False(code.IsAccessibleToUser(1, new List<Guid> { MachineB }, isEnterprise: false));
    }

    [Fact]
    public void Erisim_seviyesi_metni()
    {
        Assert.Equal("Ücretsiz", new ErrorCode { IsFreeAccess = true }.AccessLevel);
        Assert.Equal("Premium", new ErrorCode { IsFreeAccess = false }.AccessLevel);
    }
}

public class SubscriptionPlanTests
{
    [Theory]
    [InlineData(0, "Makine seçimi yok")]
    [InlineData(3, "3 makine")]
    [InlineData(-1, "Sınırsız makine")]
    public void Makine_secimi_metni(int maxSelection, string expected)
    {
        Assert.Equal(expected, new SubscriptionPlan { MaxMachineSelection = maxSelection }.MachineSelectionText);
    }

    [Fact]
    public void Fiyati_sifir_olan_plan_ucretsizdir()
    {
        Assert.True(new SubscriptionPlan { Price = 0 }.IsFreePlan);
        Assert.False(new SubscriptionPlan { Price = 299 }.IsFreePlan);
    }

    [Fact]
    public void Eksi_bir_sinirsiz_secim_demektir()
    {
        Assert.True(new SubscriptionPlan { MaxMachineSelection = -1 }.HasUnlimitedMachineSelection);
        Assert.False(new SubscriptionPlan { MaxMachineSelection = 3 }.HasUnlimitedMachineSelection);
    }
}
