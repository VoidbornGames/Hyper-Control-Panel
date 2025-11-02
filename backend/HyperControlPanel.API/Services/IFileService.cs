using HyperControlPanel.API.DTOs;

namespace HyperControlPanel.API.Services
{
    public interface IFileService
    {
        Task<FileBrowserResponseDto> BrowseFilesAsync(Guid siteId, FileBrowserRequestDto request);
        Task<bool> CreateDirectoryAsync(Guid siteId, string path, string directoryName);
        Task<bool> DeleteFileAsync(Guid siteId, string path);
        Task<bool> DeleteDirectoryAsync(Guid siteId, string path);
        Task<bool> RenameFileAsync(Guid siteId, string oldPath, string newName);
        Task<bool> CopyFileAsync(Guid siteId, string sourcePath, string targetPath);
        Task<bool> MoveFileAsync(Guid siteId, string sourcePath, string targetPath);
        Task<string?> ReadFileAsync(Guid siteId, string path);
        Task<bool> WriteFileAsync(Guid siteId, string path, string content);
        Task<string> UploadFileAsync(Guid siteId, string path, Stream fileStream, string fileName);
        Task<Stream> DownloadFileAsync(Guid siteId, string path);
        Task<bool> SetFilePermissionsAsync(Guid siteId, string path, string permissions);
        Task<long> GetDirectorySizeAsync(Guid siteId, string path);
        Task<bool> ExtractArchiveAsync(Guid siteId, string archivePath, string extractPath);
        Task<string> CreateArchiveAsync(Guid siteId, string sourcePath, string archivePath);
    }
}