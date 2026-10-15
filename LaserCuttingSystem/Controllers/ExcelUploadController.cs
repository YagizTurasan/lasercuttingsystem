using ClosedXML.Excel;
using LaserCuttingSystem.Data.Context;
using LaserCuttingSystem.Domain.Entities.Error;
using LaserCuttingSystem.Domain.Entities.Machine;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LaserCuttingSystem.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ExcelUploadController : ControllerBase
    {
        private readonly LaserCuttingDbContext _context;
        private readonly ILogger<ExcelUploadController> _logger;

        public ExcelUploadController(ILogger<ExcelUploadController> logger, LaserCuttingDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Sütun sırası ImportFromExcel'in okuduğu sırayla aynı olmalıdır
        private static readonly (string Header, string Example, string Note)[] TemplateColumns =
        {
            ("Code", "E102", "Zorunlu. Aynı kod varsa güncellenir"),
            ("Title", "Lazer gücü düşük", "Zorunlu"),
            ("Description", "Lazer kaynağı beklenen gücü üretmiyor.", ""),
            ("Solutions", "1) Lens temizliğini kontrol edin. 2) Rezonatör gazını kontrol edin.", ""),
            ("IsFreeAccess", "1", "1 = ücretsiz, 0 = premium"),
            ("Brand", "Trumpf", "Zorunlu. Yoksa makine oluşturulur"),
            ("Model", "TruLaser 3030", "Yoksa oluşturulur"),
            ("Section", "Kesim Kafası", "Model gerekli"),
            ("Part", "Lens", "Section gerekli"),
            ("ErrorSource", "Optik", "Yoksa oluşturulur"),
        };

        [HttpGet("template")]
        public IActionResult DownloadTemplate()
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Hata Kodları");

            for (int i = 0; i < TemplateColumns.Length; i++)
            {
                var (header, example, note) = TemplateColumns[i];
                var headerCell = sheet.Cell(1, i + 1);
                headerCell.Value = header;
                headerCell.Style.Font.Bold = true;
                headerCell.Style.Fill.BackgroundColor = XLColor.LightGray;
                if (!string.IsNullOrEmpty(note)) headerCell.CreateComment().AddText(note);

                sheet.Cell(2, i + 1).Value = example;
            }

            // IsFreeAccess sayı olarak okunur
            sheet.Cell(2, 5).Value = 1;

            sheet.SheetView.FreezeRows(1);
            sheet.Columns().AdjustToContents(1, 2, 10, 60);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "hata-kodlari-sablon.xlsx");
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportFromExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Lütfen bir Excel dosyası seçin");
            }

            var allowedExtensions = new[] { ".xlsx", ".xls" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest("Sadece Excel dosyaları (.xlsx, .xls) kabul edilir");
            }

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);

                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                {
                    return BadRequest("Excel dosyasında çalışma sayfası bulunamadı");
                }

                var rowCount = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                if (rowCount < 2)
                {
                    return BadRequest("Excel dosyasında veri bulunamadı");
                }

                var errors = new List<string>();
                int successCount = 0, failureCount = 0;

                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var dto = new
                        {
                            Code = worksheet.Cell(row, 1).GetValue<string>()?.Trim(),
                            Title = worksheet.Cell(row, 2).GetValue<string>()?.Trim(),
                            Description = worksheet.Cell(row, 3).GetValue<string>()?.Trim(),
                            Solutions = worksheet.Cell(row, 4).GetValue<string>()?.Trim(),
                            IsFreeAccess = worksheet.Cell(row, 5).GetValue<int>() == 1,
                            Brand = worksheet.Cell(row, 6).GetValue<string>()?.Trim(),
                            Model = worksheet.Cell(row, 7).GetValue<string>()?.Trim(),
                            Section = worksheet.Cell(row, 8).GetValue<string>()?.Trim(),
                            Part = worksheet.Cell(row, 9).GetValue<string>()?.Trim(),
                            ErrorSource = worksheet.Cell(row, 10).GetValue<string>()?.Trim(),
                            RowNumber = row
                        };

                        if (string.IsNullOrEmpty(dto.Code) ||
                            string.IsNullOrEmpty(dto.Title) ||
                            string.IsNullOrEmpty(dto.Brand))
                        {
                            errors.Add($"Satır {row}: Hata kodu, başlık ve marka alanları zorunludur");
                            failureCount++;
                            continue;
                        }

                        // Makine var mı kontrol et, yoksa oluştur
                        var machine = await _context.LaserMachines
                            .FirstOrDefaultAsync(m => m.Brand == dto.Brand);

                        if (machine == null)
                        {
                            machine = new LaserMachine
                            {
                                Id = Guid.NewGuid(),
                                Brand = dto.Brand,
                                Description = $"{dto.Brand} lazer kesim makineleri",
                                CreatedAt = DateTime.Now,
                                UpdatedAt = DateTime.Now,
                                IsActive = true
                            };
                            _context.LaserMachines.Add(machine);
                            await _context.SaveChangesAsync();
                        }

                        // Model var mı kontrol et, yoksa oluştur
                        MachineModel? model = null;
                        if (!string.IsNullOrEmpty(dto.Model))
                        {
                            model = await _context.MachineModels
                                .FirstOrDefaultAsync(m => m.MachineId == machine.Id && m.ModelCode == dto.Model);

                            if (model == null)
                            {
                                model = new MachineModel
                                {
                                    Id = Guid.NewGuid(),
                                    MachineId = machine.Id,
                                    ModelCode = dto.Model,
                                    Description = $"{dto.Brand} {dto.Model} lazer kesim makinesi",
                                    CreatedAt = DateTime.Now,
                                    UpdatedAt = DateTime.Now,
                                    IsActive = true
                                };
                                _context.MachineModels.Add(model);
                                await _context.SaveChangesAsync();
                            }
                        }

                        // Bölüm var mı kontrol et, yoksa oluştur
                        MachineSection? section = null;
                        if (!string.IsNullOrEmpty(dto.Section) && model != null)
                        {
                            section = await _context.MachineSections
                                .FirstOrDefaultAsync(s => s.MachineModelId == model.Id && s.SectionName == dto.Section);

                            if (section == null)
                            {
                                section = new MachineSection
                                {
                                    Id = Guid.NewGuid(),
                                    MachineModelId = model.Id,
                                    SectionName = dto.Section,
                                    Description = $"{dto.Section} ünitesi",
                                    CreatedAt = DateTime.Now,
                                    UpdatedAt = DateTime.Now,
                                    IsActive = true
                                };
                                _context.MachineSections.Add(section);
                                await _context.SaveChangesAsync();
                            }
                        }

                        // Parça var mı kontrol et, yoksa oluştur
                        MachinePart? part = null;
                        if (!string.IsNullOrEmpty(dto.Part) && section != null)
                        {
                            part = await _context.MachineParts
                                .FirstOrDefaultAsync(p => p.MachineSectionId == section.Id && p.PartName == dto.Part);

                            if (part == null)
                            {
                                part = new MachinePart
                                {
                                    Id = Guid.NewGuid(),
                                    MachineSectionId = section.Id,
                                    PartName = dto.Part,
                                    Description = dto.Part,
                                    CreatedAt = DateTime.Now,
                                    UpdatedAt = DateTime.Now,
                                    IsActive = true
                                };
                                _context.MachineParts.Add(part);
                                await _context.SaveChangesAsync();
                            }
                        }

                        // Hata kaynağı var mı kontrol et, yoksa oluştur
                        ErrorSource? errorSource = null;
                        if (!string.IsNullOrEmpty(dto.ErrorSource))
                        {
                            errorSource = await _context.ErrorSources
                                .FirstOrDefaultAsync(es => es.SourceName == dto.ErrorSource);

                            if (errorSource == null)
                            {
                                errorSource = new ErrorSource
                                {
                                    Id = Guid.NewGuid(),
                                    SourceName = dto.ErrorSource,
                                    Description = dto.ErrorSource,
                                    CreatedAt = DateTime.Now,
                                    UpdatedAt = DateTime.Now,
                                    IsActive = true
                                };
                                _context.ErrorSources.Add(errorSource);
                                await _context.SaveChangesAsync();
                            }
                        }

                        // Hata kodu zaten var mı kontrol et
                        var existingErrorCode = await _context.ErrorCodes
                            .FirstOrDefaultAsync(ec => ec.Code == dto.Code);

                        if (existingErrorCode != null)
                        {
                            existingErrorCode.Title = dto.Title;
                            existingErrorCode.Description = dto.Description;
                            existingErrorCode.Solutions = dto.Solutions;
                            existingErrorCode.IsFreeAccess = dto.IsFreeAccess;
                            existingErrorCode.IsCommonAcrossAllMachines = false;
                            existingErrorCode.MachineId = machine.Id;
                            existingErrorCode.MachinePartId = part?.Id;
                            existingErrorCode.ErrorSourceId = errorSource?.Id;
                            existingErrorCode.UpdatedAt = DateTime.Now;
                        }
                        else
                        {
                            var errorCode = new ErrorCode
                            {
                                Id = Guid.NewGuid(),
                                Code = dto.Code,
                                Title = dto.Title,
                                Description = dto.Description,
                                Solutions = dto.Solutions,
                                IsFreeAccess = dto.IsFreeAccess,
                                IsCommonAcrossAllMachines = false,
                                MachineId = machine.Id,
                                MachinePartId = part?.Id,
                                ErrorSourceId = errorSource?.Id,
                                CreatedAt = DateTime.Now,
                                UpdatedAt = DateTime.Now,
                                IsActive = true
                            };
                            _context.ErrorCodes.Add(errorCode);
                        }

                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Satır {row}: {ex.Message}");
                        failureCount++;
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"İşlem tamamlandı. {successCount} başarılı, {failureCount} başarısız",
                    successCount,
                    failureCount,
                    errors
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel import sırasında hata oluştu");
                return StatusCode(500, $"Genel hata: {ex.Message}");
            }
        }
    }
}


