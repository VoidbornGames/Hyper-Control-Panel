using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using HyperControlPanel.API.Data;
using HyperControlPanel.API.Models;
using MySql.Data.MySqlClient;
using System.Data;

namespace HyperControlPanel.API.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<DatabaseService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<SiteDatabase?> CreateDatabaseAsync(Site site)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("MySqlConnection")!;
                var databaseName = $"site_{site.Id:N}";
                var username = $"site_{site.Id:N}_user";
                var password = GenerateSecurePassword();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Create database
                var createDbCmd = connection.CreateCommand();
                createDbCmd.CommandText = $"CREATE DATABASE `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";
                await createDbCmd.ExecuteNonQueryAsync();

                // Create user and grant privileges
                var createUserCmd = connection.CreateCommand();
                createUserCmd.CommandText = $"CREATE USER '{username}'@'%' IDENTIFIED BY '{password}'";
                await createUserCmd.ExecuteNonQueryAsync();

                var grantCmd = connection.CreateCommand();
                grantCmd.CommandText = $"GRANT ALL PRIVILEGES ON `{databaseName}`.* TO '{username}'@'%'";
                await grantCmd.ExecuteNonQueryAsync();

                var flushCmd = connection.CreateCommand();
                flushCmd.CommandText = "FLUSH PRIVILEGES";
                await flushCmd.ExecuteNonQueryAsync();

                var database = new SiteDatabase
                {
                    SiteId = site.Id,
                    DatabaseName = databaseName,
                    Username = username,
                    Password = password,
                    Host = "mysql",
                    Port = 3306,
                    DatabaseType = "mysql"
                };

                _context.SiteDatabases.Add(database);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created database {DatabaseName} for site {SiteId}", databaseName, site.Id);
                return database;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating database for site {SiteId}", site.Id);
                return null;
            }
        }

        public async Task<bool> DropDatabaseAsync(Guid databaseId)
        {
            try
            {
                var database = await _context.SiteDatabases.FindAsync(databaseId);
                if (database == null)
                {
                    return false;
                }

                var connectionString = _configuration.GetConnectionString("MySqlConnection")!;
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Drop database
                var dropDbCmd = connection.CreateCommand();
                dropDbCmd.CommandText = $"DROP DATABASE IF EXISTS `{database.DatabaseName}`";
                await dropDbCmd.ExecuteNonQueryAsync();

                // Drop user
                var dropUserCmd = connection.CreateCommand();
                dropUserCmd.CommandText = $"DROP USER IF EXISTS '{database.Username}'@'%'";
                await dropUserCmd.ExecuteNonQueryAsync();

                // Remove from database
                _context.SiteDatabases.Remove(database);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Dropped database {DatabaseName}", database.DatabaseName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dropping database {DatabaseId}", databaseId);
                return false;
            }
        }

        public async Task<SiteDatabase?> CloneDatabaseAsync(Guid sourceDatabaseId, Guid targetSiteId)
        {
            try
            {
                var sourceDatabase = await _context.SiteDatabases.FindAsync(sourceDatabaseId);
                if (sourceDatabase == null)
                {
                    return null;
                }

                var targetSite = await _context.Sites.FindAsync(targetSiteId);
                if (targetSite == null)
                {
                    return null;
                }

                // Create new database
                var newDatabase = await CreateDatabaseAsync(targetSite);
                if (newDatabase == null)
                {
                    return null;
                }

                // Copy data from source to target
                await CopyDatabaseData(sourceDatabase, newDatabase);

                return newDatabase;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cloning database {DatabaseId}", sourceDatabaseId);
                return null;
            }
        }

        public async Task<bool> TestDatabaseConnectionAsync(Guid databaseId)
        {
            try
            {
                var database = await _context.SiteDatabases.FindAsync(databaseId);
                if (database == null)
                {
                    return false;
                }

                var connectionString = GetDatabaseConnectionString(database);
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                return connection.State == ConnectionState.Open;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing database connection for {DatabaseId}", databaseId);
                return false;
            }
        }

        public async Task<string?> GetDatabaseConnectionStringAsync(Guid databaseId)
        {
            var database = await _context.SiteDatabases.FindAsync(databaseId);
            return database != null ? GetDatabaseConnectionString(database) : null;
        }

        public async Task<bool> BackupDatabaseAsync(Guid databaseId, string backupPath)
        {
            try
            {
                var database = await _context.SiteDatabases.FindAsync(databaseId);
                if (database == null)
                {
                    return false;
                }

                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "mysqldump",
                        Arguments = $"-h {database.Host} -P {database.Port} -u {database.Username} -p{database.Password} {database.DatabaseName} > {backupPath}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync();

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error backing up database {DatabaseId}", databaseId);
                return false;
            }
        }

        public async Task<bool> RestoreDatabaseAsync(Guid databaseId, string backupPath)
        {
            try
            {
                var database = await _context.SiteDatabases.FindAsync(databaseId);
                if (database == null)
                {
                    return false;
                }

                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "mysql",
                        Arguments = $"-h {database.Host} -P {database.Port} -u {database.Username} -p{database.Password} {database.DatabaseName} < {backupPath}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync();

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring database {DatabaseId}", databaseId);
                return false;
            }
        }

        public async Task<List<string>> GetDatabaseTablesAsync(Guid databaseId)
        {
            var tables = new List<string>();
            try
            {
                var database = await _context.SiteDatabases.FindAsync(databaseId);
                if (database == null)
                {
                    return tables;
                }

                var connectionString = GetDatabaseConnectionString(database);
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var cmd = connection.CreateCommand();
                cmd.CommandText = "SHOW TABLES";
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    tables.Add(reader.GetString(0));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting database tables for {DatabaseId}", databaseId);
            }

            return tables;
        }

        public async Task<long> GetDatabaseSizeAsync(Guid databaseId)
        {
            try
            {
                var database = await _context.SiteDatabases.FindAsync(databaseId);
                if (database == null)
                {
                    return 0;
                }

                var connectionString = GetDatabaseConnectionString(database);
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var cmd = connection.CreateCommand();
                cmd.CommandText = $"SELECT SUM(data_length + index_length) FROM information_schema.tables WHERE table_schema = '{database.DatabaseName}'";
                var result = await cmd.ExecuteScalarAsync();

                return result != DBNull.Value ? Convert.ToInt64(result) : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting database size for {DatabaseId}", databaseId);
                return 0;
            }
        }

        public async Task<bool> CreateDatabaseUserAsync(string username, string password, string databaseName)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("MySqlConnection")!;
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var cmd = connection.CreateCommand();
                cmd.CommandText = $"CREATE USER '{username}'@'%' IDENTIFIED BY '{password}'";
                await cmd.ExecuteNonQueryAsync();

                return await GrantDatabasePrivilegesAsync(username, databaseName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating database user {Username}", username);
                return false;
            }
        }

        public async Task<bool> GrantDatabasePrivilegesAsync(string username, string databaseName)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("MySqlConnection")!;
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var cmd = connection.CreateCommand();
                cmd.CommandText = $"GRANT ALL PRIVILEGES ON `{databaseName}`.* TO '{username}'@'%'";
                await cmd.ExecuteNonQueryAsync();

                var flushCmd = connection.CreateCommand();
                flushCmd.CommandText = "FLUSH PRIVILEGES";
                await flushCmd.ExecuteNonQueryAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error granting privileges for user {Username}", username);
                return false;
            }
        }

        private string GetDatabaseConnectionString(SiteDatabase database)
        {
            return $"Server={database.Host};Port={database.Port};Database={database.DatabaseName};Uid={database.Username};Pwd={database.Password};";
        }

        private string GenerateSecurePassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            var password = new char[16];

            for (int i = 0; i < password.Length; i++)
            {
                password[i] = chars[random.Next(chars.Length)];
            }

            return new string(password);
        }

        private async Task CopyDatabaseData(SiteDatabase source, SiteDatabase target)
        {
            var sourceConnectionString = GetDatabaseConnectionString(source);
            var targetConnectionString = GetDatabaseConnectionString(target);

            // This is a simplified approach - in production you'd use mysqldump/mysql for this
            _logger.LogInformation("Copying data from {SourceDatabase} to {TargetDatabase}", source.DatabaseName, target.DatabaseName);

            // Implementation would depend on your specific requirements
            await Task.Delay(100); // Placeholder
        }
    }
}