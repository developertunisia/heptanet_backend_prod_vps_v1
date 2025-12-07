using HeptaNet.Application.Services.FileStorage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FileInfo = HeptaNet.Application.Services.FileStorage.FileInfo;

namespace HeptaNet.Infrastructure.FileStorage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly string _basePath;

    public LocalFileStorageService(
        IWebHostEnvironment environment,
        ILogger<LocalFileStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
        _basePath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads");
        
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<string> SaveFileAsync(IFormFile file, string folder, int userId)
    {
        try
        {
            var userFolder = Path.Combine(_basePath, folder, userId.ToString());
            if (!Directory.Exists(userFolder))
            {
                Directory.CreateDirectory(userFolder);
            }

            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(userFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativeUrl = $"/uploads/{folder}/{userId}/{fileName}";
            _logger.LogInformation($"File saved: {relativeUrl}");
            
            return relativeUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving file: {file.FileName}");
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
            var filePath = fileUrl.Replace("/uploads/", "").Replace("/", "\\");
            var fullPath = Path.Combine(_basePath, filePath);
            
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting file: {fileUrl}");
            return false;
        }
    }

    public string GetFileUrl(string fileName, string folder)
    {
        return $"/uploads/{folder}/{fileName}";
    }

    public async Task<FileInfo> GetFileInfoAsync(string fileUrl)
    {
        var filePath = fileUrl.Replace("/uploads/", "").Replace("/", "\\");
        var fullPath = Path.Combine(_basePath, filePath);
        
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found: {fileUrl}");
        }

        var systemFileInfo = new System.IO.FileInfo(fullPath);
        return new FileInfo
        {
            FileName = systemFileInfo.Name,
            FileSize = systemFileInfo.Length,
            ContentType = GetContentType(systemFileInfo.Extension)
        };
    }

    private string GetContentType(string extension)
    {
        return extension.ToLower() switch
        {
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".m4a" => "audio/mp4",
            ".aac" => "audio/aac",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".mp4" => "video/mp4",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}

