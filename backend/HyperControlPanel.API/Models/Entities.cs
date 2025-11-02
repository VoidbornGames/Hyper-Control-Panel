using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HyperControlPanel.API.Models
{
    // User entity extending IdentityUser for additional fields
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }
    }

    // Site entity
    public class Site
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(255)]
        public string Domain { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string Platform { get; set; } = string.Empty; // wordpress, hugo, laravel, etc.

        [Required]
        [StringLength(255)]
        public string Template { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "creating"; // creating, active, suspended, error, deleting

        public int StorageLimitGB { get; set; } = 10;
        public long StorageUsedMB { get; set; } = 0;

        public string? SiteDirectory { get; set; }
        public string? ContainerName { get; set; }
        public string? ContainerId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastBackupAt { get; set; }

        // Navigation properties
        public virtual ICollection<Domain> Domains { get; set; } = new List<Domain>();
        public virtual ICollection<SiteDatabase> Databases { get; set; } = new List<SiteDatabase>();
        public virtual ICollection<Deployment> Deployments { get; set; } = new List<Deployment>();
    }

    // Domain entity
    public class Domain
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid SiteId { get; set; }

        [ForeignKey("SiteId")]
        public virtual Site Site { get; set; } = null!;

        [Required]
        [StringLength(255)]
        public string DomainName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty; // subdomain, custom

        public bool SslEnabled { get; set; } = false;
        public DateTime? SslExpiresAt { get; set; }
        public bool DnsVerified { get; set; } = false;
        public string? DnsVerificationToken { get; set; }
        public bool IsPrimary { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    // Site Database entity
    public class SiteDatabase
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid SiteId { get; set; }

        [ForeignKey("SiteId")]
        public virtual Site Site { get; set; } = null!;

        [Required]
        [StringLength(255)]
        public string DatabaseName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Host { get; set; } = string.Empty;

        public int Port { get; set; } = 3306;
        public string DatabaseType { get; set; } = "mysql"; // mysql, postgresql

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // Template entity
    public class Template
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        public string Platform { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string TemplatePath { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? ScreenshotUrl { get; set; }

        [StringLength(1000)]
        public string? PreviewUrl { get; set; }

        public string? ManifestJson { get; set; } // JSON configuration
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;
        public int SortOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    // Deployment entity
    public class Deployment
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid SiteId { get; set; }

        [ForeignKey("SiteId")]
        public virtual Site Site { get; set; } = null!;

        [Required]
        [StringLength(255)]
        public string Type { get; set; } = string.Empty; // create, update, delete, backup, restore

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "pending"; // pending, running, completed, failed

        [StringLength(1000)]
        public string? Message { get; set; }

        public string? LogOutput { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // Site backup entity
    public class SiteBackup
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid SiteId { get; set; }

        [ForeignKey("SiteId")]
        public virtual Site Site { get; set; } = null!;

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public long FileSizeBytes { get; set; }

        [Required]
        [StringLength(255)]
        public string FilePath { get; set; } = string.Empty;

        [StringLength(50)]
        public string Type { get; set; } = "manual"; // manual, automatic

        [StringLength(1000)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
    }
}