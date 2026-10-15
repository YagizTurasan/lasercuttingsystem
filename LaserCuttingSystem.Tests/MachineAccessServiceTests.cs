using System.Linq.Expressions;
using LaserCuttingSystem.Business.Services.DomainService;
using LaserCuttingSystem.Data.IRepositories;
using LaserCuttingSystem.Domain.Entities.Error;
using LaserCuttingSystem.Domain.Entities.Machine;
using LaserCuttingSystem.Domain.Entities.Subscription;
using LaserCuttingSystem.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LaserCuttingSystem.Tests;

public class MachineAccessServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _machineA = Guid.NewGuid();
    private readonly Guid _machineB = Guid.NewGuid();

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserSubscriptionRepository> _subscriptions = new();
    private readonly Mock<IUserMachineSelectionRepository> _selections = new();
    private readonly Mock<IErrorCodeRepository> _errorCodes = new();
    private readonly Mock<ILaserMachineRepository> _machines = new();

    public MachineAccessServiceTests()
    {
        _unitOfWork.SetupGet(u => u.UserSubscriptions).Returns(_subscriptions.Object);
        _unitOfWork.SetupGet(u => u.UserMachineSelections).Returns(_selections.Object);
        _unitOfWork.SetupGet(u => u.ErrorCodes).Returns(_errorCodes.Object);
        _unitOfWork.SetupGet(u => u.LaserMachines).Returns(_machines.Object);
        GivenSelectedMachines();
    }

    private MachineAccessService CreateService() =>
        new(_unitOfWork.Object, NullLogger<MachineAccessService>.Instance);

    private void GivenPlan(int level, int maxSelection, bool allMachines = false)
    {
        _subscriptions.Setup(r => r.GetActiveSubscriptionByUserAsync(_userId)).ReturnsAsync(new UserSubscription
        {
            UserId = _userId,
            Status = SubscriptionStatus.Active,
            EndDate = DateTime.UtcNow.AddMonths(1),
            Plan = new SubscriptionPlan { Level = level, MaxMachineSelection = maxSelection, CanAccessAllMachines = allMachines }
        });
    }

    private void GivenSelectedMachines(params Guid[] machineIds)
    {
        _selections.Setup(r => r.GetUserSelectionsAsync(_userId))
            .ReturnsAsync(machineIds.Select(id => new UserMachineSelection { UserId = _userId, MachineId = id }).ToList());
    }

    private void GivenErrorCode(ErrorCode errorCode)
    {
        _errorCodes.Setup(r => r.GetByIdAsync(errorCode.Id, It.IsAny<Expression<Func<ErrorCode, object>>[]>()))
            .ReturnsAsync(errorCode);
    }

    [Fact]
    public async Task Abonelik_yoksa_makineye_erisim_yok()
    {
        _subscriptions.Setup(r => r.GetActiveSubscriptionByUserAsync(_userId)).ReturnsAsync((UserSubscription?)null);

        Assert.False(await CreateService().CanUserAccessMachine(_userId, _machineA));
    }

    [Fact]
    public async Task Ucretsiz_plan_hicbir_makineye_erisemez()
    {
        GivenPlan(level: 0, maxSelection: 0);
        GivenSelectedMachines(_machineA);

        Assert.False(await CreateService().CanUserAccessMachine(_userId, _machineA));
    }

    [Fact]
    public async Task Kurumsal_plan_tum_makinelere_erisir()
    {
        GivenPlan(level: 3, maxSelection: -1, allMachines: true);

        Assert.True(await CreateService().CanUserAccessMachine(_userId, _machineB));
    }

    [Fact]
    public async Task Temel_plan_yalnizca_secilen_makineye_erisir()
    {
        GivenPlan(level: 1, maxSelection: 1);
        GivenSelectedMachines(_machineA);
        var service = CreateService();

        Assert.True(await service.CanUserAccessMachine(_userId, _machineA));
        Assert.False(await service.CanUserAccessMachine(_userId, _machineB));
    }

    [Fact]
    public async Task Secim_limiti_asildiysa_erisim_yok()
    {
        GivenPlan(level: 1, maxSelection: 1);
        GivenSelectedMachines(_machineA, _machineB);

        Assert.False(await CreateService().CanUserAccessMachine(_userId, _machineA));
    }

    [Fact]
    public async Task Erisilebilir_makineler_limite_gore_kirpilir()
    {
        GivenPlan(level: 1, maxSelection: 1);
        GivenSelectedMachines(_machineA, _machineB);

        var ids = await CreateService().GetAccessibleMachineIds(_userId);

        Assert.Equal(new[] { _machineA }, ids);
    }

    [Fact]
    public async Task Ucretsiz_hata_kodu_aboneliksiz_de_gorulur()
    {
        var code = new ErrorCode { Code = "E1", Title = "T", IsFreeAccess = true, MachineId = _machineB };
        GivenErrorCode(code);
        _subscriptions.Setup(r => r.GetActiveSubscriptionByUserAsync(_userId)).ReturnsAsync((UserSubscription?)null);

        Assert.True(await CreateService().CanUserAccessErrorCode(_userId, code.Id));
    }

    [Fact]
    public async Task Pasif_hata_koduna_erisim_yok()
    {
        var code = new ErrorCode { Code = "E1", Title = "T", IsFreeAccess = true, IsActive = false };
        GivenErrorCode(code);

        Assert.False(await CreateService().CanUserAccessErrorCode(_userId, code.Id));
    }

    [Fact]
    public async Task Premium_hata_kodu_parca_uzerinden_secilen_makineye_baglanir()
    {
        // Kod doğrudan makineye değil, Makine > Model > Bölüm > Parça zincirindeki parçaya bağlı
        var part = new MachinePart
        {
            PartName = "Lens",
            MachineSection = new MachineSection { MachineModel = new MachineModel { MachineId = _machineA } }
        };
        var code = new ErrorCode { Code = "E2", Title = "T", MachinePart = part };
        GivenErrorCode(code);
        GivenPlan(level: 1, maxSelection: 1);
        GivenSelectedMachines(_machineA);

        Assert.True(await CreateService().CanUserAccessErrorCode(_userId, code.Id));
    }

    [Fact]
    public async Task Premium_hata_kodu_secilmeyen_makinedeyse_erisim_yok()
    {
        var code = new ErrorCode { Code = "E3", Title = "T", MachineId = _machineB };
        GivenErrorCode(code);
        GivenPlan(level: 1, maxSelection: 1);
        GivenSelectedMachines(_machineA);

        Assert.False(await CreateService().CanUserAccessErrorCode(_userId, code.Id));
    }
}
