using HeptaNet.Application.Services.Audio;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace HeptaNet.Infrastructure.Audio;

public class AudioMetadataService : IAudioMetadataService
{
    private readonly ILogger<AudioMetadataService> _logger;

    public AudioMetadataService(ILogger<AudioMetadataService> logger)
    {
        _logger = logger;
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
}

