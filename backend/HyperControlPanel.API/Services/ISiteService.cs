using HyperControlPanel.API.DTOs;
using HyperControlPanel.API.Models;

namespace HyperControlPanel.API.Services
{
    public interface ISiteService
    {
        Task<bool> CreateSiteAsync(Guid siteId);
        Task<bool> DeleteSiteAsync(Guid siteId);
        Task<Guid> CreateBackupAsync(Guid siteId, CreateBackupRequestDto request);
        Task<Guid> CloneSiteAsync(Guid sourceSiteId, CloneSiteRequestDto request);
        Task<bool> RestartSiteAsync(Guid siteId);
        Task<Site?> GetSiteAsync(Guid siteId);
        Task<IEnumerable<Site>> GetUserSitesAsync(string userId);
        Task<bool> UpdateSiteStorageUsageAsync(Guid siteId);
        Task<Deployment> CreateDeploymentAsync(Guid siteId, string type, string userId);
        Task<bool> UpdateDeploymentStatusAsync(Guid deploymentId, string status, string? message = null);
        Task<string?> GetSiteLogsAsync(Guid siteId);
    }
}