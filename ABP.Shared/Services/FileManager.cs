using ABP.Application.Interfaces.Services;

namespace ABP.Shared.Services
{
    public class FileManager : IFileManager
    {
        private static readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        public string? Upload<TKey>(Stream? file, TKey id, string folderName, string? fileName, bool isEditMode = false, string? imagePath = "")
        {
            if (isEditMode && file is null)
                return imagePath;

            if (file is null || file.Length <= 0)
                return "El archivo seleccionado está vacío o corrupto.";


            if (file.Length > 5242880)
            {
                return "La imagen seleccionada no puede superar los 5mb.";
            }

            var idString = id?.ToString();
            if (string.IsNullOrWhiteSpace(idString))
                return string.Empty;

            if (string.IsNullOrWhiteSpace(fileName))
                return string.Empty;

            var extension = Path.GetExtension(fileName)?.ToLowerInvariant() ?? string.Empty;
            if (!_allowedExtensions.Contains(extension))
                return null;

            if (!IsValidImageMagicBytes(file))
                return null;

            string basePath = $"Images/{folderName}/{idString}";
            string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", basePath);

            if (!Directory.Exists(absolutePath))
                Directory.CreateDirectory(absolutePath);

            string newFileName = $"{Guid.NewGuid()}{extension}";
            string fullFilePath = Path.Combine(absolutePath, newFileName);

            using (var stream = new FileStream(fullFilePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            if (isEditMode && !string.IsNullOrWhiteSpace(imagePath))
            {
                string[] parts = imagePath.Split('/');
                string oldFileName = parts[^1];
                string oldFullPath = Path.Combine(absolutePath, oldFileName);

                if (File.Exists(oldFullPath))
                    File.Delete(oldFullPath);
            }

            return $"/{basePath}/{newFileName}";
        }

        private static bool IsValidImageMagicBytes(Stream stream)
        {
            var originalPosition = stream.Position;
            Span<byte> header = stackalloc byte[12];
            var bytesRead = stream.Read(header);

            stream.Position = originalPosition;

            if (bytesRead < 12)
                return false;

            // JPEG: FF D8 FF
            if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                return true;

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E &&
                header[3] == 0x47 && header[4] == 0x0D && header[5] == 0x0A &&
                header[6] == 0x1A && header[7] == 0x0A)
                return true;

            // WebP: RIFF .... WEBP (bytes 0-3 = RIFF, bytes 8-11 = WEBP)
            if (header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 &&
                header[3] == 0x46 && header[8] == 0x57 && header[9] == 0x45 &&
                header[10] == 0x42 && header[11] == 0x50)
                return true;

            return false;
        }

        public bool DeleteFile(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return false;

            string webRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));
            string relativePath = imagePath
                .TrimStart('/', '\\')
                .Replace('/', Path.DirectorySeparatorChar);
            string fullPath = Path.GetFullPath(Path.Combine(webRoot, relativePath));

            if (!fullPath.StartsWith(webRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!File.Exists(fullPath))
                return false;

            File.Delete(fullPath);
            return true;
        }

        public bool Delete<TKey>(TKey id, string folderName)
        {
            var idString = id?.ToString();
            if (string.IsNullOrWhiteSpace(idString))
                return false;

            string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", folderName, idString);

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                return true;
            }

            return false;
        }
    }
}