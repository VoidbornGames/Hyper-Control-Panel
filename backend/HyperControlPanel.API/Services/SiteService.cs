using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using HyperControlPanel.API.Data;
using HyperControlPanel.API.DTOs;
using HyperControlPanel.API.Models;

namespace HyperControlPanel.API.Services
{
    public class SiteService : ISiteService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDatabaseService _databaseService;
        private readonly IDomainService _domainService;
        private readonly IDockerService _dockerService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SiteService> _logger;

        public SiteService(
            ApplicationDbContext context,
            IDatabaseService databaseService,
            IDomainService domainService,
            IDockerService dockerService,
            IConfiguration configuration,
            ILogger<SiteService> logger)
        {
            _context = context;
            _databaseService = databaseService;
            _domainService = domainService;
            _dockerService = dockerService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> CreateSiteAsync(Guid siteId)
        {
            var deployment = await CreateDeploymentAsync(siteId, "create", "system");

            try
            {
                await UpdateDeploymentStatusAsync(deployment.Id, "running", "Starting site creation process");

                var site = await _context.Sites
                    .Include(s => s.Domains)
                    .Include(s => s.Databases)
                    .FirstOrDefaultAsync(s => s.Id == siteId);

                if (site == null)
                {
                    await UpdateDeploymentStatusAsync(deployment.Id, "failed", "Site not found");
                    return false;
                }

                // Step 1: Create site directory structure
                await UpdateDeploymentStatusAsync(deployment.Id, "running", "Creating site directory structure");
                var siteDirectory = await CreateSiteDirectory(site);
                site.SiteDirectory = siteDirectory;

                // Step 2: Create database
                await UpdateDeploymentStatusAsync(deployment.Id, "running", "Creating site database");
                var database = await _databaseService.CreateDatabaseAsync(site);
                if (database != null)
                {
                    site.Databases.Add(database);
                }

                // Step 3: Deploy template
                await UpdateDeploymentStatusAsync(deployment.Id, "running", "Deploying site template");
                await DeployTemplate(site);

                // Step 4: Create and start container
                await UpdateDeploymentStatusAsync(deployment.Id, "running", "Creating site container");
                var containerInfo = await _dockerService.CreateSiteContainer(site);
                site.ContainerName = containerInfo.ContainerName;
                site.ContainerId = containerInfo.ContainerId;

                // Step 5: Configure domain/DNS
                await UpdateDeploymentStatusAsync(deployment.Id, "running", "Configuring domain and SSL");
                await _domainService.ConfigureDomainAsync(site.Domains.First().Id);

                // Update site status
                site.Status = "active";
                site.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await UpdateDeploymentStatusAsync(deployment.Id, "completed", "Site created successfully");

                _logger.LogInformation("Site {SiteId} created successfully", siteId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating site {SiteId}", siteId);
                await UpdateDeploymentStatusAsync(deployment.Id, "failed", $"Error: {ex.Message}");

                // Update site status to error
                var site = await _context.Sites.FindAsync(siteId);
                if (site != null)
                {
                    site.Status = "error";
                    await _context.SaveChangesAsync();
                }

                return false;
            }
        }

        public async Task<bool> DeleteSiteAsync(Guid siteId)
        {
            var deployment = await CreateDeploymentAsync(siteId, "delete", "system");

            try
            {
                await UpdateDeploymentStatusAsync(deployment.Id, "running", "Starting site deletion process");

                var site = await _context.Sites
                    .Include(s => s.Domains)
                    .Include(s => s.Databases)
                    .FirstOrDefaultAsync(s => s.Id == siteId);

                if (site == null)
                {
                    await UpdateDeploymentStatusAsync(deployment.Id, "failed", "Site not found");
                    return false;
                }

                // Step 1: Stop and remove container
                if (!string.IsNullOrEmpty(site.ContainerId))
                {
                    await UpdateDeploymentStatusAsync(deployment.Id, "running", "Removing site container");
                    await _dockerService.RemoveContainer(site.ContainerId);
                }

                // Step 2: Remove SSL certificates
                await UpdateDeploymentStatusAsync(deployment.Id, "running", "Removing SSL certificates");
                foreach (var domain in site.Domains)
                {
                    await _domainService.RemoveSslCertificate(domain.DomainName);
                }

                // Step 3: Drop databases
                await UpdateDeploymentStatusAsync(deployment.Id, "running", "Dropping site databases");
                foreach (var database in site.Databases)
                {
                    await _databaseService.DropDatabaseAsync(database.Id);
                }

                // Step 4: Remove site directory
                await UpdateDeploymentStatusAsync(deployment.Id, "running", "Removing site files");
                await RemoveSiteDirectory(site);

                // Update site status to deleted (keep record for audit)
                site.Status = "deleted";
                site.ContainerId = null;
                site.ContainerName = null;
                await _context.SaveChangesAsync();

                await UpdateDeploymentStatusAsync(deployment.Id, "completed", "Site deleted successfully");

                _logger.LogInformation("Site {SiteId} deleted successfully", siteId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting site {SiteId}", siteId);
                await UpdateDeploymentStatusAsync(deployment.Id, "failed", $"Error: {ex.Message}");
                return false;
            }
        }

        public async Task<Guid> CreateBackupAsync(Guid siteId, CreateBackupRequestDto request)
        {
            var deployment = await CreateDeploymentAsync(siteId, "backup", "system");

            try
            {
                await UpdateDeploymentStatusAsync(deployment.Id, "running", "Creating backup");

                var site = await _context.Sites
                    .Include(s => s.Databases)
                    .FirstOrDefaultAsync(s => s.Id == siteId);

                if (site == null)
                {
                    throw new Exception("Site not found");
                }

                var backupPath = _configuration["Storage:BackupRoot"] ?? "/var/backups/hypercontrolpanel";
                var fileName = $"{site.Name}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.tar.gz";
                var filePath = Path.Combine(backupPath, fileName);

                // Create backup
                var fileSize = await CreateSiteBackup(site, filePath, request);

                // Save backup record
                var backup = new SiteBackup
                {
                    SiteId = siteId,
                    FileName = fileName,
                    FileSizeBytes = fileSize,
                    FilePath = filePath,
                    Type = "manual",
                    Description = request.Description,
                    ExpiresAt = DateTime.UtcNow.AddDays(30) // Keep for 30 days
                };

                _context.SiteBackups.Add(backup);
                await _context.SaveChangesAsync();

                // Update last backup time
                site.LastBackupAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await UpdateDeploymentStatusAsync(deployment.Id, "completed", "Backup created successfully");

                return backup.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating backup for site {SiteId}", siteId);
                await UpdateDeploymentStatusAsync(deployment.Id, "failed", $"Error: {ex.Message}");
                throw;
            }
        }

        public async Task<Guid> CloneSiteAsync(Guid sourceSiteId, CloneSiteRequestDto request)
        {
            var deployment = await CreateDeploymentAsync(sourceSiteId, "clone", "system");

            try
            {
                var sourceSite = await _context.Sites
                    .Include(s => s.Domains)
                    .Include(s => s.Databases)
                    .FirstOrDefaultAsync(s => s.Id == sourceSiteId);

                if (sourceSite == null)
                {
                    throw new Exception("Source site not found");
                }

                // Check if domain already exists
                var existingDomain = await _context.Sites
                    .AnyAsync(s => s.Domain == request.Domain || s.Domains.Any(d => d.DomainName == request.Domain));

                if (existingDomain)
                {
                    throw new Exception("Domain already exists");
                }

                await UpdateDeploymentStatusAsync(deployment.Id, "running", "Creating cloned site");

                // Create new site
                var newSite = new Site
                {
                    Name = request.Name,
                    Description = request.Description,
                    Domain = request.Domain,
                    UserId = sourceSite.UserId,
                    Platform = sourceSite.Platform,
                    Template = sourceSite.Template,
                    StorageLimitGB = sourceSite.StorageLimitGB,
                    Status = "creating"
                };

                _context.Sites.Add(newSite);
                await _context.SaveChangesAsync();

                // Add primary domain
                var primaryDomain = new Domain
                {
                    SiteId = newSite.Id,
                    DomainName = request.Domain,
                    Type = "subdomain",
                    IsPrimary = true
                };

                _context.Domains.Add(primaryDomain);
                await _context.SaveChangesAsync();

                await UpdateDeploymentStatusAsync(deployment.Id, "running", "Cloning site data");

                // Clone site files
                if (request.CloneFiles)
                {
                    await CloneSiteFiles(sourceSite, newSite);
                }

                // Clone database
                if (request.CloneDatabase)
                {
                    foreach (var sourceDb in sourceSite.Databases)
                    {
                        var clonedDb = await _databaseService.CloneDatabaseAsync(sourceDb.Id, newSite.Id);
                        if (clonedDb != null)
                        {
                            newSite.Databases.Add(clonedDb);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // Start site creation process for cloned site
                _ = Task.Run(async () => await CreateSiteAsync(newSite.Id));

                await UpdateDeploymentStatusAsync(deployment.Id, "completed", "Site cloned successfully");

                return newSite.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cloning site {SiteId}", sourceSiteId);
                await UpdateDeploymentStatusAsync(deployment.Id, "failed", $"Error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> RestartSiteAsync(Guid siteId)
        {
            var deployment = await CreateDeploymentAsync(siteId, "restart", "system");

            try
            {
                await UpdateDeploymentStatusAsync(deployment.Id, "running", "Restarting site services");

                var site = await _context.Sites.FindAsync(siteId);
                if (site == null || string.IsNullOrEmpty(site.ContainerId))
                {
                    throw new Exception("Site or container not found");
                }

                await _dockerService.RestartContainer(site.ContainerId);

                await UpdateDeploymentStatusAsync(deployment.Id, "completed", "Site restarted successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restarting site {SiteId}", siteId);
                await UpdateDeploymentStatusAsync(deployment.Id, "failed", $"Error: {ex.Message}");
                return false;
            }
        }

        public async Task<Site?> GetSiteAsync(Guid siteId)
        {
            return await _context.Sites
                .Include(s => s.Domains)
                .Include(s => s.Databases)
                .FirstOrDefaultAsync(s => s.Id == siteId);
        }

        public async Task<IEnumerable<Site>> GetUserSitesAsync(string userId)
        {
            return await _context.Sites
                .Include(s => s.Domains)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> UpdateSiteStorageUsageAsync(Guid siteId)
        {
            try
            {
                var site = await _context.Sites.FindAsync(siteId);
                if (site == null || string.IsNullOrEmpty(site.SiteDirectory))
                {
                    return false;
                }

                var directoryInfo = new DirectoryInfo(site.SiteDirectory);
                var size = directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);

                site.StorageUsedMB = size / (1024 * 1024);
                site.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating storage usage for site {SiteId}", siteId);
                return false;
            }
        }

        public async Task<Deployment> CreateDeploymentAsync(Guid siteId, string type, string userId)
        {
            var deployment = new Deployment
            {
                SiteId = siteId,
                Type = type,
                Status = "pending",
                UserId = userId
            };

            _context.Deployments.Add(deployment);
            await _context.SaveChangesAsync();

            return deployment;
        }

        public async Task<bool> UpdateDeploymentStatusAsync(Guid deploymentId, string status, string? message = null)
        {
            try
            {
                var deployment = await _context.Deployments.FindAsync(deploymentId);
                if (deployment == null)
                {
                    return false;
                }

                deployment.Status = status;
                deployment.Message = message;

                if (status == "running" && deployment.StartedAt == null)
                {
                    deployment.StartedAt = DateTime.UtcNow;
                }
                else if (status == "completed" || status == "failed")
                {
                    deployment.CompletedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating deployment status for deployment {DeploymentId}", deploymentId);
                return false;
            }
        }

        public async Task<string?> GetSiteLogsAsync(Guid siteId)
        {
            try
            {
                var site = await _context.Sites.FindAsync(siteId);
                if (site == null || string.IsNullOrEmpty(site.ContainerId))
                {
                    return null;
                }

                return await _dockerService.GetContainerLogs(site.ContainerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting logs for site {SiteId}", siteId);
                return null;
            }
        }

        // Private helper methods
        private async Task<string> CreateSiteDirectory(Site site)
        {
            var sitesRoot = _configuration["Storage:SitesRoot"] ?? "/var/www/sites";
            var siteDirectory = Path.Combine(sitesRoot, site.Id.ToString());

            Directory.CreateDirectory(siteDirectory);
            Directory.CreateDirectory(Path.Combine(siteDirectory, "public"));
            Directory.CreateDirectory(Path.Combine(siteDirectory, "private"));
            Directory.CreateDirectory(Path.Combine(siteDirectory, "logs"));

            return siteDirectory;
        }

        private async Task RemoveSiteDirectory(Site site)
        {
            if (!string.IsNullOrEmpty(site.SiteDirectory) && Directory.Exists(site.SiteDirectory))
            {
                try
                {
                    Directory.Delete(site.SiteDirectory, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not remove site directory {Directory}", site.SiteDirectory);
                }
            }
        }

        private async Task DeployTemplate(Site site)
        {
            var templatePath = Path.Combine("/templates", site.Platform, site.Template);

            if (Directory.Exists(templatePath))
            {
                // Copy template files to site directory
                var sourceDir = new DirectoryInfo(templatePath);
                var targetDir = new DirectoryInfo(site.SiteDirectory!);

                await CopyDirectory(sourceDir, targetDir);

                // Run template installation script if exists
                var installScript = Path.Combine(site.SiteDirectory!, "install.sh");
                if (File.Exists(installScript))
                {
                    await RunInstallationScript(installScript, site);
                }
            }
        }

        private async Task CopyDirectory(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (var dir in source.GetDirectories())
            {
                var targetSubDir = target.CreateSubdirectory(dir.Name);
                await CopyDirectory(dir, targetSubDir);
            }

            foreach (var file in source.GetFiles())
            {
                var targetFile = Path.Combine(target.FullName, file.Name);
                file.CopyTo(targetFile, true);
            }
        }

        private async Task RunInstallationScript(string scriptPath, Site site)
        {
            // This would execute the installation script with site-specific parameters
            // Implementation would depend on your security requirements
            _logger.LogInformation("Running installation script for site {SiteId}", site.Id);
        }

        private async Task<long> CreateSiteBackup(Site site, string filePath, CreateBackupRequestDto request)
        {
            // Create tar.gz backup of site files and optionally database
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "tar",
                    Arguments = $"-czf \"{filePath}\" -C \"{Path.GetDirectoryName(site.SiteDirectory)}\" \"{Path.GetFileName(site.SiteDirectory)}\"",
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
                throw new Exception($"Backup creation failed: {process.StandardError.ReadToEnd()}");
            }

            var fileInfo = new FileInfo(filePath);
            return fileInfo.Length;
        }

        private async Task CloneSiteFiles(Site sourceSite, Site targetSite)
        {
            if (string.IsNullOrEmpty(sourceSite.SiteDirectory) || string.IsNullOrEmpty(targetSite.SiteDirectory))
            {
                return;
            }

            var sourceDir = new DirectoryInfo(sourceSite.SiteDirectory);
            var targetDir = new DirectoryInfo(targetSite.SiteDirectory);

            await CopyDirectory(sourceDir, targetDir);
        }
    }
}