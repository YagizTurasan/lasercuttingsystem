using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.ValueObjects
{
    public record FileInfo
    {
        public string Url { get; }
        public string FileName { get; }
        public string ContentType { get; }
        public long FileSize { get; }
        public DateTime UploadedAt { get; }

        public FileInfo(string url, string fileName, string contentType, long fileSize, DateTime? uploadedAt = null)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be null or empty", nameof(url));

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

            if (fileSize <= 0)
                throw new ArgumentException("File size must be greater than zero", nameof(fileSize));

            Url = url;
            FileName = fileName;
            ContentType = contentType ?? "application/octet-stream";
            FileSize = fileSize;
            UploadedAt = uploadedAt ?? DateTime.UtcNow;
        }

        public string GetFileSizeFormatted()
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = FileSize;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        public bool IsImage() => ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

        public bool IsVideo() => ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);

        public bool IsPdf() => ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
    }
}
