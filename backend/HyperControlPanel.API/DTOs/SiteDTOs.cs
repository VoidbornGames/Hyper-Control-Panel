using System.ComponentModel.DataAnnotations;

namespace HyperControlPanel.API.DTOs
{
    // Site DTOs
    public class CreateSiteRequestDto
    {
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(255)]
        public string Domain { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Platform { get; set; } = string.Empty; // wordpress, hugo, laravel, etc.

        [Required]
        [StringLength(255)]
        public string Template { get; set; } = string.Empty;

        [Range(1, 100)]
        public int StorageLimitGB { get; set; } = 10;

        public List<string>? CustomDomains { get; set; } = new List<string>();
    }

    public class UpdateSiteRequestDto
    {
        [StringLength(255)]
        public string? Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(1, 100)]
        public int? StorageLimitGB { get; set; }
    }

    public class SiteDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Domain { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string Template { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int StorageLimitGB { get; set; }
        public long StorageUsedMB { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastBackupAt { get; set; }
        public List<DomainDto> Domains { get; set; } = new List<DomainDto>();
        public List<DatabaseDto> Databases { get; set; } = new List<DatabaseDto>();
        public string Url { get; set; } = string.Empty;
        public bool IsAccessible { get; set; }
    }

    public class SiteListDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public long StorageUsedMB { get; set; }
        public int StorageLimitGB { get; set; }
        public string Url { get; set; } = string.Empty;
        public bool IsAccessible { get; set; }
        public int DomainCount { get; set; }
        public bool HasSsl { get; set; }
    }

    // Domain DTOs
    public class AddDomainRequestDto
    {
        [Required]
        [StringLength(255)]
        public string DomainName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty; // subdomain, custom

        public bool IsPrimary { get; set; } = false;
    }

    public class DomainDto
    {
        public Guid Id { get; set; }
        public Guid SiteId { get; set; }
        public string DomainName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool SslEnabled { get; set; }
        public DateTime? SslExpiresAt { get; set; }
        public bool DnsVerified { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime CreatedAt { get; set; }
        public int DaysUntilSslExpiry { get; set; }
        public string SslStatus { get; set; } = string.Empty;
    }

    // Database DTOs
    public class DatabaseDto
    {
        public Guid Id { get; set; }
        public string DatabaseName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string DatabaseType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // Template DTOs
    public class TemplateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Platform { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? ScreenshotUrl { get; set; }
        public string? PreviewUrl { get; set; }
        public bool IsFeatured { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TemplateFilterDto
    {
        public string? Platform { get; set; }
        public string? Category { get; set; }
        public bool FeaturedOnly { get; set; } = false;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // Deployment DTOs
    public class DeploymentDto
    {
        public Guid Id { get; set; }
        public Guid SiteId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Duration { get; set; } = string.Empty;
        public bool IsRunning { get; set; }
    }

    // Backup DTOs
    public class CreateBackupRequestDto
    {
        [StringLength(1000)]
        public string? Description { get; set; }
        public bool IncludeDatabase { get; set; } = true;
        public bool IncludeFiles { get; set; } = true;
    }

    public class BackupDto
    {
        public Guid Id { get; set; }
        public Guid SiteId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string FileSizeDisplay { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsExpired { get; set; }
    }

    // File management DTOs
    public class FileBrowserRequestDto
    {
        public string Path { get; set; } = "/";
        public bool ShowHidden { get; set; } = false;
    }

    public class FileItemDto
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
        public string SizeDisplay { get; set; } = string.Empty;
        public DateTime ModifiedAt { get; set; }
        public string Permissions { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
    }

    public class FileBrowserResponseDto
    {
        public string CurrentPath { get; set; } = string.Empty;
        public List<FileItemDto> Items { get; set; } = new List<FileItemDto>();
        public List<string> Breadcrumbs { get; set; } = new List<string>();
        public long TotalSize { get; set; }
        public string TotalSizeDisplay { get; set; } = string.Empty;
    }

    // Statistics DTOs
    public class SiteStatsDto
    {
        public int TotalSites { get; set; }
        public int ActiveSites { get; set; }
        public int SuspendedSites { get; set; }
        public long TotalStorageUsedMB { get; set; }
        public int TotalDomains { get; set; }
        public int DomainsWithSsl { get; set; }
        public int TotalDatabases { get; set; }
        public DateTime LastActivity { get; set; }
    }

    public class DashboardStatsDto
    {
        public SiteStatsDto SiteStats { get; set; } = new SiteStatsDto();
        public List<RecentActivityDto> RecentActivity { get; set; } = new List<RecentActivityDto>();
        public List<SiteListDto> RecentSites { get; set; } = new List<SiteListDto>();
        public List<TemplateDto> PopularTemplates { get; set; } = new List<TemplateDto>();
    }

    public class RecentActivityDto
    {
        public string Type { get; set; } = string.Empty; // site_created, site_updated, backup_created, etc.
        public string Description { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string RelativeTime { get; set; } = string.Empty;
    }
}