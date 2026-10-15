using System.ComponentModel.DataAnnotations;

namespace LaserCuttingSystem.WebApi
{
    public class ExcelUploadRequest
    {
        [Required]
        public IFormFile File { get; set; } = null!;
    }
}
