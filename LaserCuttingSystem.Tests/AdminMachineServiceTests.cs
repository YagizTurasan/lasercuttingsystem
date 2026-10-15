using System.Linq.Expressions;
using LaserCuttingSystem.Business.Services.AdminServices;
using LaserCuttingSystem.Data.IRepositories;
using LaserCuttingSystem.Domain.Entities.Error;
using LaserCuttingSystem.Domain.Entities.Machine;
using Moq;

namespace LaserCuttingSystem.Tests;

public class AdminMachineServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IMachineModelRepository> _models = new();
    private readonly Mock<IMachinePartRepository> _parts = new();

    public AdminMachineServiceTests()
    {
        _unitOfWork.SetupGet(u => u.MachineModels).Returns(_models.Object);
        _unitOfWork.SetupGet(u => u.MachineParts).Returns(_parts.Object);
    }

    private AdminMachineService CreateService() => new(_unitOfWork.Object);

    [Fact]
    public async Task Model_geri_getirilince_silmenin_kapattigi_bolumler_de_acilir()
    {
        var model = new MachineModel { ModelCode = "M1", IsActive = false };
        model.MachineSections.Add(new MachineSection { SectionName = "S1", IsActive = false });
        _models.Setup(r => r.GetByIdAsync(model.Id, It.IsAny<Expression<Func<MachineModel, object>>[]>())).ReturnsAsync(model);

        var result = await CreateService().RestoreMachineModelAsync(model.Id);

        Assert.True(result.Success);
        Assert.True(model.IsActive);
        Assert.All(model.MachineSections, s => Assert.True(s.IsActive));
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Parca_geri_getirilince_bagli_hata_kodlari_da_acilir()
    {
        var part = new MachinePart { PartName = "Lens", IsActive = false };
        part.ErrorCodes.Add(new ErrorCode { Code = "E1", Title = "T", IsActive = false });
        _parts.Setup(r => r.GetByIdAsync(part.Id, It.IsAny<Expression<Func<MachinePart, object>>[]>())).ReturnsAsync(part);

        var result = await CreateService().RestoreMachinePartAsync(part.Id);

        Assert.True(result.Success);
        Assert.True(part.IsActive);
        Assert.All(part.ErrorCodes, e => Assert.True(e.IsActive));
    }

    [Fact]
    public async Task Olmayan_oge_geri_getirilemez()
    {
        _models.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Expression<Func<MachineModel, object>>[]>()))
            .ReturnsAsync((MachineModel?)null);

        var result = await CreateService().RestoreMachineModelAsync(Guid.NewGuid());

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Hiyerarsi_listesi_silinmis_ogeleri_de_icerir_aktifler_once_gelir()
    {
        var machineId = Guid.NewGuid();
        _models.Setup(r => r.FindAsync(It.IsAny<Expression<Func<MachineModel, bool>>>())).ReturnsAsync(new[]
        {
            new MachineModel { MachineId = machineId, ModelCode = "B", IsActive = false },
            new MachineModel { MachineId = machineId, ModelCode = "A", IsActive = true },
        });

        var items = await CreateService().GetMachineModelsAsync(machineId);

        Assert.Equal(new[] { "A", "B" }, items.Select(i => i.Name));
        Assert.Equal(new[] { true, false }, items.Select(i => i.IsActive));
    }
}
