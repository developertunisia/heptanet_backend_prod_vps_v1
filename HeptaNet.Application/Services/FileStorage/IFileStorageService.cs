using Microsoft.AspNetCore.Http;

namespace HeptaNet.Application.Services.FileStorage;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string folder, int userId);
    Task<bool> DeleteFileAsync(string fileUrl);
    string GetFileUrl(string fileName, string folder);
    Task<FileInfo> GetFileInfoAsync(string fileUrl);
}

public class FileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int? DurationSeconds { get; set; }
}

