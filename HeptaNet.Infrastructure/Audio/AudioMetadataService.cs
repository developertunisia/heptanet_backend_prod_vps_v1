using HeptaNet.Application.Services.Audio;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using NAudio.Wave;
using Microsoft.Extensions.Hosting;

namespace HeptaNet.Infrastructure.Audio;

public class AudioMetadataService : IAudioMetadataService
{
    private readonly ILogger<AudioMetadataService> _logger;
    private readonly IWebHostEnvironment _environment;

    public AudioMetadataService(ILogger<AudioMetadataService> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async Task<int?> GetAudioDurationAsync(IFormFile audioFile)
    {
        var tempPath = string.Empty;
        try
        {
            tempPath = Path.GetTempFileName();
            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await audioFile.CopyToAsync(stream);
            }

            using (var audioFileReader = new AudioFileReader(tempPath))
            {
                var duration = (int)audioFileReader.TotalTime.TotalSeconds;
                
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                
                return duration;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Could not extract audio duration for {audioFile.FileName}");
            
            if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch { }
            }
            
            return null;
        }
    }

    public async Task<int?> GetAudioDurationFromFilePathAsync(string filePath)
    {
        try
        {
            // Le filePath devrait être quelque chose comme: /uploads/voice-messages/3/file.m4a
            // Il faut le convertir en chemin physique du serveur
            
            string physicalPath;
            
            // Vérifier si on est en production Docker (chemin /app/uploads)
            if (_environment.IsProduction() && Directory.Exists("/app/uploads"))
            {
                // En production Docker, utiliser toujours /app/uploads (volume monté)
                var relativePath = filePath.Replace("/uploads/", "", StringComparison.OrdinalIgnoreCase);
                var safePath = Path.Combine(relativePath.Split('/'));
                physicalPath = Path.Combine("/app/uploads", safePath);
                _logger.LogInformation($"Production/Docker mode: Using {physicalPath} for duration calculation");
            }
            else
            {
                // En développement, utiliser le chemin standard
                var basePath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads");
                var relativePath = filePath.Replace("/uploads/", "", StringComparison.OrdinalIgnoreCase);
                var safePath = Path.Combine(relativePath.Split('/'));
                physicalPath = Path.Combine(basePath, safePath);
                _logger.LogInformation($"Development mode: Using {physicalPath} for duration calculation");
            }

            if (!File.Exists(physicalPath))
            {
                _logger.LogWarning($"Audio file not found at path: {physicalPath} (from URL: {filePath})");
                return null;
            }

            using (var audioFileReader = new AudioFileReader(physicalPath))
            {
                var duration = (int)audioFileReader.TotalTime.TotalSeconds;
                _logger.LogInformation($"Duration calculated: {duration} seconds for file: {filePath}");
                return duration;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Could not extract audio duration from file path: {filePath}");
            return null;
        }
    }
}

