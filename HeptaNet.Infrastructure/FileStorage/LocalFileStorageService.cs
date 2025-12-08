using HeptaNet.Application.Services.FileStorage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FileInfo = HeptaNet.Application.Services.FileStorage.FileInfo;
using System;  // ✅ AJOUTER
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

        // ✅ FORCER le chemin vers le volume monté en production
        var isProduction = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";

        if (isProduction)
        {
            // En production Docker, utiliser toujours /app/uploads (volume monté)
            _basePath = "/app/uploads";
            _logger.LogInformation("Production mode: Using /app/uploads for file storage");
        }
        else
        {
            // En développement, utiliser le chemin relatif
            _basePath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads");
            _logger.LogInformation($"Development mode: Using {_basePath} for file storage");
        }

        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
            _logger.LogInformation($"Created directory: {_basePath}");
        }

        _logger.LogInformation($"File storage initialized at: {_basePath}");
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
            //var filePath = fileUrl.Replace("/uploads/", "").Replace("/", "\\"); // separator for windows 
            // 1. Enlever le préfixe URL
            string relativePath = fileUrl.Replace("/uploads/", "");
            string filePath = Path.Combine("uploads", Path.Combine(relativePath.Split('/')));
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
        // var filePath = fileUrl.Replace("/uploads/", "").Replace("/", "\\"); //for windows 
        var filePath = fileUrl
                .Replace("/uploads/", "")
                .Replace("/", Path.DirectorySeparatorChar.ToString());

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

