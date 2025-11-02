using HyperControlPanel.API.Models;

namespace HyperControlPanel.API.Services
{
    public interface IDatabaseService
    {
        Task<SiteDatabase?> CreateDatabaseAsync(Site site);
        Task<bool> DropDatabaseAsync(Guid databaseId);
        Task<SiteDatabase?> CloneDatabaseAsync(Guid sourceDatabaseId, Guid targetSiteId);
        Task<bool> TestDatabaseConnectionAsync(Guid databaseId);
        Task<string?> GetDatabaseConnectionStringAsync(Guid databaseId);
        Task<bool> BackupDatabaseAsync(Guid databaseId, string backupPath);
        Task<bool> RestoreDatabaseAsync(Guid databaseId, string backupPath);
        Task<List<string>> GetDatabaseTablesAsync(Guid databaseId);
        Task<long> GetDatabaseSizeAsync(Guid databaseId);
        Task<bool> CreateDatabaseUserAsync(string username, string password, string databaseName);
        Task<bool> GrantDatabasePrivilegesAsync(string username, string databaseName);
    }
}