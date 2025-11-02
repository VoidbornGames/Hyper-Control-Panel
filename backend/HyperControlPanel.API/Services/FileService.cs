using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HyperControlPanel.API.Data;
using HyperControlPanel.API.DTOs;

namespace HyperControlPanel.API.Services
{
    public class FileService : IFileService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FileService> _logger;

        public FileService(
            ApplicationDbContext context,
            ILogger<FileService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<FileBrowserResponseDto> BrowseFilesAsync(Guid siteId, FileBrowserRequestDto request)
        {
            try
            {
                var site = await _context.Sites.FindAsync(siteId);
                if (site == null || string.IsNullOrEmpty(site.SiteDirectory))
                {
                    throw new ArgumentException("Site not found or site directory not configured");
                }

                var fullPath = Path.Combine(site.SiteDirectory, request.Path.TrimStart('/'));

                if (!Directory.Exists(fullPath))
                {
                    throw new DirectoryNotFoundException("Directory not found");
                }

                var items = new List<FileItemDto>();
                var totalSize = 0L;

                foreach (var entry in Directory.GetFileSystemEntries(fullPath))
                {
                    var fileInfo = new FileInfo(entry);
                    var isDirectory = Directory.Exists(entry);
                    var name = Path.GetFileName(entry);

                    // Skip hidden files unless requested
                    if (!request.ShowHidden && name.StartsWith('.'))
                    {
                        continue;
                    }

                    var item = new FileItemDto
                    {
                        Name = name,
                        Path = Path.Combine(request.Path, name).Replace('\\', '/'),
                        IsDirectory = isDirectory,
                        Size = isDirectory ? 0 : fileInfo.Length,
                        SizeDisplay = isDirectory ? "-" : FormatFileSize(fileInfo.Length),
                        ModifiedAt = fileInfo.LastWriteTime,
                        Permissions = GetFilePermissions(entry),
                        Extension = isDirectory ? "" : fileInfo.Extension.ToLower()
                    };

                    items.Add(item);
                    totalSize += item.Size;
                }

                // Sort items: directories first, then files, both alphabetically
                items = items.OrderBy(i => i.IsDirectory ? 0 : 1).ThenBy(i => i.Name).ToList();

                return new FileBrowserResponseDto
                {
                    CurrentPath = request.Path,
                    Items = items,
                    Breadcrumbs = GetBreadcrumbs(request.Path),
                    TotalSize = totalSize,
                    TotalSizeDisplay = FormatFileSize(totalSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error browsing files for site {SiteId} at path {Path}", siteId, request.Path);
                throw;
            }
        }

        public async Task<bool> CreateDirectoryAsync(Guid siteId, string path, string directoryName)
        {
            try
            {
                var site = await _context.Sites.FindAsync(siteId);
                if (site == null || string.IsNullOrEmpty(site.SiteDirectory))
                {
                    return false;
                }

                var fullPath = Path.Combine(site.SiteDirectory, path.TrimStart('/'), directoryName);

                if (Directory.Exists(fullPath))
                {
                    return false; // Directory already exists
                }

                Directory.CreateDirectory(fullPath);
                _logger.LogInformation("Created directory {DirectoryPath} for site {SiteId}", fullPath, siteId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating directory for site {SiteId}", siteId);
                return false;
            }
        }

        public async Task<bool> DeleteFileAsync(Guid siteId, string path)
        {
            try
            {
                var site = await _context.Sites.FindAsync(siteId);
                if (site == null || string.IsNullOrEmpty(site.SiteDirectory))
                {
                    return false;
                }

                var fullPath = Path.Combine(site.SiteDirectory, path.TrimStart('/'));

                if (!File.Exists(fullPath))
                {
                    return false;
                }

                File.Delete(fullPath);
                _logger.LogInformation("Deleted file {FilePath} for site {SiteId}", fullPath, siteId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file for site {SiteId}", siteId);
                return false;
            }
        }

        public async Task<bool> DeleteDirectoryAsync(Guid siteId, string path)
        {
            try
            {
                var site = await _context.Sites.FindAsync(siteId);
                if (site == null || string.IsNullOrEmpty(site.SiteDirectory))
                {
                    return false;
                }

                var fullPath = Path.Combine(site.SiteDirectory, path.TrimStart('/'));

                if (!Directory.Exists(fullPath))
                {
                    return false;
                }

                Directory.Delete(fullPath, true);
                _logger.LogInformation("Deleted directory {DirectoryPath} for site {SiteId}", fullPath, siteId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting directory for site {SiteId}", siteId);
                return false;
            }
        }

        public async Task<bool> RenameFileAsync(Guid siteId, string oldPath, string newName)
        {
            try
            {
                var site = await _context.Sites.FindAsync(siteId);
                if (site == null || string.IsNullOrEmpty(site.SiteDirectory))
                {
                    return false;
                }

                var fullPath = Path.Combine(site.SiteDirectory, oldPath.TrimStart('/'));
                var newPath = Path.Combine(Path.GetDirectoryName(fullPath)!, newName);

                if (!File.Exists(fullPath) || File.Exists(newPath))
                {
                    return false;
                }

                File.Move(fullPath, newPath);
                _logger.LogInformation("Renamed file from {OldPath} to {NewPath} for site {SiteId}", fullPath, newPath, siteId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renaming file for site {SiteId}", siteId);
                return false;
            }
        }

        public async Task<bool> CopyFileAsync(Guid siteId, string sourcePath, string targetPath)
        {
            try
            {
                var site = await _context.Sites.FindAsync(siteId);
                if (site == null || string.IsNullOrEmpty(site.SiteDirectory))
                {
                    return false;
                }

                var fullSourcePath = Path.Combine(site.SiteDirectory, sourcePath.TrimStart('/'));
                var fullTargetPath = Path.Combine(site.SiteDirectory, targetPath.TrimStart('/'));

                if (!File.Exists(fullSourcePath))
                {
                    return false;
                }

                // Ensure target directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(fullTargetPath)!);

                File.Copy(fullSourcePath, fullTargetPath, true);
                _logger.LogInformation("Copied file from {SourcePath} to {TargetPath} for site {SiteId}", fullSourcePath, fullTargetPath, siteId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying file for site {SiteId}", siteId);
                return false;
            }
        }

        public async Task<bool> MoveFileAsync(Guid siteId, string sourcePath, string targetPath)
        {
            try
            {
                var site = await _context.Sites.FindAsync(siteId);
                if (site == null || string.IsNullOrEmpty(site.SiteDirectory))
                {
                    return false;
                }

                var fullSourcePath = Path.Combine(site.SiteDirectory, sourcePath.TrimStart('/'));
                var fullTargetPath = Path.Combine(site.SiteDirectory, targetPath.TrimStart('/'));

                if (!File.Exists(fullSourcePath))
                {
                    return false;
                }

                // Ensure target directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(fullTargetPath)!);

                File.Move(fullSourcePath, fullTargetPath);
                _logger.LogInformation("Moved file from {SourcePath} to {TargetPath} for site {SiteId}", fullSourcePath, fullTargetPath, siteId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving file for site {SiteId}", siteId);
                return false;
            }
        }

        public async Task<string?> ReadFileAsync(Guid siteId, string path)
        {
            try
            {
                var site = await _context.Sites.FindAsync(siteId);
                if (site == null || string.IsNullOrEmpty(site.SiteDirectory))
                {
                    return null;
                }

                var fullPath = Path.Combine(site.SiteDirectory, path.TrimStart('/'));

                if (!File.Exists(fullPath))
                {
                    return null;
                }

                // Only allow reading text files up to 10MB
                var fileInfo = new FileInfo(fullPath);
                if (fileInfo.Length > 10 * 1024 * 1024)
                {
                    throw new InvalidOperationException("File too large to read");
                }

                return await File.ReadAllTextAsync(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading file for site {SiteId}", siteId);
                return null;
            }
        }

        public async Task<bool> WriteFileAsync(Guid siteId, string path, string content)
        {
            try
            {
                var site = await _context.Sites.FindAsync(siteId);
                if (site == null || string.IsNullOrEmpty(site.SiteDirectory))
                {
                    return false;
                }

                var fullPath = Path.Combine(site.SiteDirectory, path.TrimStart('/'));

                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

                await File.WriteAllTextAsync(fullPath, content);
                _logger.LogInformation("Wrote file {FilePath} for site {SiteId}", fullPath, siteId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing file for site {SiteId}", siteId);
                return false;
            }
        }

        public async Task<string> UploadFileAsync(Guid siteId, string path, Stream fileStream, string fileName)
        {
            try
            {
                var site = await _context.Sites.FindAsync(siteId);
                if (site == null || string.IsNullOrEmpty(site.SiteDirectory))
                {
                    throw new ArgumentException("Site not found or site directory not configured");
                }

                var fullPath = Path.Combine(site.SiteDirectory, path.TrimStart('/'), fileName);

                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

                using var fileStreamOutput = new FileStream(fullPath, FileMode.Create);
                await fileStream.CopyToAsync(fileStreamOutput);

                _logger.LogInformation("Uploaded file {FilePath} for site {SiteId}", fullPath, siteId);
                return fullPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file for site {SiteId}", siteId);
                throw;
            }
        }

        public async Task<Stream> DownloadFileAsync(Guid siteId, string path)
        {
            try
            {
                var site = await _context.Sites.FindAsync(siteId);
                if (site == null || string.IsNullOrEmpty(site.SiteDirectory))
                {
                    throw new ArgumentException("Site not found or site directory not configured");
                }

                var fullPath = Path.Combine(site.SiteDirectory, path.TrimStart('/'));

                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException("File not found");
                }

                return new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file for site {SiteId}", siteId);
                throw;
            }
        }

        public async Task<bool> SetFilePermissionsAsync(Guid siteId, string path, string permissions)
        {
            try
            {
                var site = await _context.Sites.FindAsync(siteId);
                if (site == null || string.IsNullOrEmpty(site.SiteDirectory))
                {
                    return false;
                }

                var fullPath = Path.Combine(site.SiteDirectory, path.TrimStart('/'));

                if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
                {
                    return false;
                }

                // Parse permissions (e.g., "755", "644")
                if (int.TryParse(permissions, out var mode))
                {
                    var unixMode = (UnixFileMode)mode;
                    if (File.Exists(fullPath))
                    {
                        File.SetUnixFileMode(fullPath, unixMode);
                    }
                    else
                    {
                        Directory.SetUnixFileMode(fullPath, unixMode);
                    }

                    _logger.LogInformation("Set permissions {Permissions} on {Path} for site {SiteId}", permissions, fullPath, siteId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting file permissions for site {SiteId}", siteId);
                return false;
            }
        }

        public async Task<long> GetDirectorySizeAsync(Guid siteId, string path)
        {
            try
            {
                var site = await _context.Sites.FindAsync(siteId);
                if (site == null || string.IsNullOrEmpty(site.SiteDirectory))
                {
                    return 0;
                }

                var fullPath = Path.Combine(site.SiteDirectory, path.TrimStart('/'));

                if (!Directory.Exists(fullPath))
                {
                    return 0;
                }

                return await Task.Run(() => Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting directory size for site {SiteId}", siteId);
                return 0;
            }
        }

        public async Task<bool> ExtractArchiveAsync(Guid siteId, string archivePath, string extractPath)
        {
            try
            {
                var site = await _context.Sites.FindAsync(siteId);
                if (site == null || string.IsNullOrEmpty(site.SiteDirectory))
                {
                    return false;
                }

                var fullArchivePath = Path.Combine(site.SiteDirectory, archivePath.TrimStart('/'));
                var fullExtractPath = Path.Combine(site.SiteDirectory, extractPath.TrimStart('/'));

                if (!File.Exists(fullArchivePath))
                {
                    return false;
                }

                // Ensure extract directory exists
                Directory.CreateDirectory(fullExtractPath);

                // Use system tar command for extraction
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "tar",
                        Arguments = $"-xf \"{fullArchivePath}\" -C \"{fullExtractPath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync();

                var success = process.ExitCode == 0;
                if (success)
                {
                    _logger.LogInformation("Extracted archive {ArchivePath} to {ExtractPath} for site {SiteId}", fullArchivePath, fullExtractPath, siteId);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting archive for site {SiteId}", siteId);
                return false;
            }
        }

        public async Task<string> CreateArchiveAsync(Guid siteId, string sourcePath, string archivePath)
        {
            try
            {
                var site = await _context.Sites.FindAsync(siteId);
                if (site == null || string.IsNullOrEmpty(site.SiteDirectory))
                {
                    throw new ArgumentException("Site not found or site directory not configured");
                }

                var fullSourcePath = Path.Combine(site.SiteDirectory, sourcePath.TrimStart('/'));
                var fullArchivePath = Path.Combine(site.SiteDirectory, archivePath.TrimStart('/'));

                if (!Directory.Exists(fullSourcePath) && !File.Exists(fullSourcePath))
                {
                    throw new DirectoryNotFoundException("Source path not found");
                }

                // Ensure archive directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(fullArchivePath)!);

                // Use system tar command for compression
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "tar",
                        Arguments = $"-czf \"{fullArchivePath}\" -C \"{Path.GetDirectoryName(fullSourcePath)}\" \"{Path.GetFileName(fullSourcePath)}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Archive creation failed: {process.StandardError.ReadToEnd()}");
                }

                _logger.LogInformation("Created archive {ArchivePath} from {SourcePath} for site {SiteId}", fullArchivePath, fullSourcePath, siteId);
                return fullArchivePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating archive for site {SiteId}", siteId);
                throw;
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }

            return $"{number:n1} {suffixes[counter]}";
        }

        private List<string> GetBreadcrumbs(string path)
        {
            var breadcrumbs = new List<string>();
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

            var currentPath = "";
            foreach (var part in parts)
            {
                currentPath += "/" + part;
                breadcrumbs.Add(currentPath);
            }

            return breadcrumbs;
        }

        private string GetFilePermissions(string path)
        {
            try
            {
                if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                {
                    var fileInfo = new FileInfo(path);
                    var mode = fileInfo.UnixFileMode;
                    return ((int)mode).ToString("D3");
                }
                else
                {
                    // On Windows, return a default representation
                    return "644";
                }
            }
            catch
            {
                return "644";
            }
        }
    }
}