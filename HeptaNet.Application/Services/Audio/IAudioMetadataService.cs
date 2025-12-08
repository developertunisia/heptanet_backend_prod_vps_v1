using Microsoft.AspNetCore.Http;

namespace HeptaNet.Application.Services.Audio;

public interface IAudioMetadataService
{
    Task<int?> GetAudioDurationAsync(IFormFile audioFile);
    Task<int?> GetAudioDurationFromFilePathAsync(string filePath);
}

