using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Net;

namespace HyperControlPanel.API.Services
{
    public class SslService : ISslService
    {
        private readonly ILogger<SslService> _logger;
        private readonly IConfiguration _configuration;

        public SslService(
            ILogger<SslService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<bool> RequestSslCertificateAsync(string domainName, string email)
        {
            try
            {
                _logger.LogInformation("Requesting SSL certificate for domain {DomainName}", domainName);

                // In a real implementation, this would use Certbot/Let's Encrypt API
                // For demo purposes, we'll simulate the certificate creation

                // Check if domain is accessible first
                if (!await IsDomainAccessibleAsync(domainName))
                {
                    _logger.LogWarning("Domain {DomainName} is not accessible, cannot issue certificate", domainName);
                    return false;
                }

                // Simulate certificate creation process
                await Task.Delay(2000); // Simulate API call

                // Create self-signed certificate for demo (in production, use Let's Encrypt)
                var success = await CreateSelfSignedCertificate(domainName);

                if (success)
                {
                    _logger.LogInformation("SSL certificate created successfully for {DomainName}", domainName);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting SSL certificate for {DomainName}", domainName);
                return false;
            }
        }

        public async Task<bool> RenewSslCertificateAsync(string domainName)
        {
            try
            {
                _logger.LogInformation("Renewing SSL certificate for domain {DomainName}", domainName);

                // Check existing certificate expiry
                var expiryDate = await GetCertificateExpiryAsync(domainName);
                if (expiryDate == null)
                {
                    _logger.LogWarning("No existing certificate found for {DomainName}", domainName);
                    return false;
                }

                // Only renew if certificate expires within 30 days
                if (expiryDate.Value > DateTime.UtcNow.AddDays(30))
                {
                    _logger.LogInformation("Certificate for {DomainName} does not need renewal yet", domainName);
                    return true;
                }

                // Simulate renewal process
                await Task.Delay(2000);

                _logger.LogInformation("SSL certificate renewed successfully for {DomainName}", domainName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renewing SSL certificate for {DomainName}", domainName);
                return false;
            }
        }

        public async Task<bool> RevokeSslCertificateAsync(string domainName)
        {
            try
            {
                _logger.LogInformation("Revoking SSL certificate for domain {DomainName}", domainName);

                // In production, this would call Let's Encrypt API to revoke
                // For demo, just remove the certificate files
                var certPath = $"/etc/ssl/certs/{domainName}.crt";
                var keyPath = $"/etc/ssl/private/{domainName}.key";

                if (File.Exists(certPath))
                {
                    File.Delete(certPath);
                }

                if (File.Exists(keyPath))
                {
                    File.Delete(keyPath);
                }

                await Task.Delay(500); // Simulate API call

                _logger.LogInformation("SSL certificate revoked successfully for {DomainName}", domainName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking SSL certificate for {DomainName}", domainName);
                return false;
            }
        }

        public async Task<DateTime?> GetCertificateExpiryAsync(string domainName)
        {
            try
            {
                var certPath = $"/etc/ssl/certs/{domainName}.crt";
                if (!File.Exists(certPath))
                {
                    return null;
                }

                var cert = new X509Certificate2(certPath);
                return cert.NotAfter;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting certificate expiry for {DomainName}", domainName);
                return null;
            }
        }

        public async Task<bool> InstallCertificateAsync(string domainName, string certificatePath, string keyPath)
        {
            try
            {
                _logger.LogInformation("Installing SSL certificate for {DomainName}", domainName);

                var installCertPath = $"/etc/ssl/certs/{domainName}.crt";
                var installKeyPath = $"/etc/ssl/private/{domainName}.key";

                // Copy certificate files to SSL directory
                File.Copy(certificatePath, installCertPath, true);
                File.Copy(keyPath, installKeyPath, true);

                // Set appropriate permissions
                File.SetUnixFileMode(installCertPath, UnixFileMode.UserRead | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
                File.SetUnixFileMode(installKeyPath, UnixFileMode.UserRead);

                // Update Nginx configuration
                await UpdateNginxConfiguration(domainName);

                // Reload Nginx
                await ReloadNginx();

                await Task.Delay(1000);

                _logger.LogInformation("SSL certificate installed successfully for {DomainName}", domainName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error installing SSL certificate for {DomainName}", domainName);
                return false;
            }
        }

        public async Task<bool> CreateWildcardCertificateAsync(string baseDomain, string email)
        {
            try
            {
                _logger.LogInformation("Creating wildcard certificate for {BaseDomain}", baseDomain);

                // In production, this would use DNS-01 challenge with Let's Encrypt
                // For demo, create a self-signed wildcard certificate
                var wildcardDomain = $"*.{baseDomain}";

                var success = await CreateSelfSignedCertificate(wildcardDomain);

                if (success)
                {
                    _logger.LogInformation("Wildcard SSL certificate created successfully for {BaseDomain}", baseDomain);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating wildcard certificate for {BaseDomain}", baseDomain);
                return false;
            }
        }

        public async Task<bool> IsCertificateValidAsync(string domainName)
        {
            try
            {
                var expiryDate = await GetCertificateExpiryAsync(domainName);
                if (expiryDate == null)
                {
                    return false;
                }

                return expiryDate.Value > DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking certificate validity for {DomainName}", domainName);
                return false;
            }
        }

        public async Task<string?> GetCertificateDetailsAsync(string domainName)
        {
            try
            {
                var certPath = $"/etc/ssl/certs/{domainName}.crt";
                if (!File.Exists(certPath))
                {
                    return null;
                }

                var cert = new X509Certificate2(certPath);

                var details = $"Subject: {cert.Subject}\n" +
                             $"Issuer: {cert.Issuer}\n" +
                             $"Valid From: {cert.NotBefore}\n" +
                             $"Valid Until: {cert.NotAfter}\n" +
                             $"Serial Number: {cert.SerialNumber}\n" +
                             $"Thumbprint: {cert.Thumbprint}";

                return details;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting certificate details for {DomainName}", domainName);
                return null;
            }
        }

        public async Task<List<string>> GetExpiringCertificatesAsync(int daysThreshold = 30)
        {
            var expiringDomains = new List<string>();

            try
            {
                var sslDir = "/etc/ssl/certs";
                if (!Directory.Exists(sslDir))
                {
                    return expiringDomains;
                }

                var certFiles = Directory.GetFiles(sslDir, "*.crt");
                var thresholdDate = DateTime.UtcNow.AddDays(daysThreshold);

                foreach (var certFile in certFiles)
                {
                    try
                    {
                        var cert = new X509Certificate2(certFile);
                        if (cert.NotAfter <= thresholdDate)
                        {
                            var domainName = Path.GetFileNameWithoutExtension(certFile);
                            expiringDomains.Add(domainName);
                        }
                    }
                    catch
                    {
                        // Skip invalid certificates
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expiring certificates");
            }

            return await Task.FromResult(expiringDomains);
        }

        private async Task<bool> CreateSelfSignedCertificate(string domainName)
        {
            try
            {
                // In a real implementation, this would use OpenSSL or similar
                // For demo purposes, we'll create placeholder certificate files

                var certPath = $"/etc/ssl/certs/{domainName}.crt";
                var keyPath = $"/etc/ssl/private/{domainName}.key";

                // Ensure directories exist
                Directory.CreateDirectory("/etc/ssl/certs");
                Directory.CreateDirectory("/etc/ssl/private");

                // Create placeholder certificate (in production, use OpenSSL)
                await File.WriteAllTextAsync(certPath, $"-----BEGIN CERTIFICATE-----\nPlaceholder certificate for {domainName}\n-----END CERTIFICATE-----");
                await File.WriteAllTextAsync(keyPath, $"-----BEGIN PRIVATE KEY-----\nPlaceholder private key for {domainName}\n-----END PRIVATE KEY-----");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating self-signed certificate for {DomainName}", domainName);
                return false;
            }
        }

        private async Task<bool> IsDomainAccessibleAsync(string domainName)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                var response = await httpClient.GetAsync($"http://{domainName}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task UpdateNginxConfiguration(string domainName)
        {
            var configPath = $"/etc/nginx/sites-available/{domainName}";
            var configContent = $@"
server {{
    listen 80;
    server_name {domainName};
    return 301 https://$server_name$request_uri;
}}

server {{
    listen 443 ssl http2;
    server_name {domainName};

    ssl_certificate /etc/ssl/certs/{domainName}.crt;
    ssl_certificate_key /etc/ssl/private/{domainName}.key;

    include /etc/nginx/snippets/ssl-params.conf;

    location / {{
        proxy_pass http://localhost:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }}
}}";

            await File.WriteAllTextAsync(configPath, configContent);

            // Enable site
            var enabledPath = $"/etc/nginx/sites-enabled/{domainName}";
            if (!File.Exists(enabledPath))
            {
                File.CreateSymbolicLink(enabledPath, configPath);
            }
        }

        private async Task ReloadNginx()
        {
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "nginx",
                        Arguments = "-s reload",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading Nginx");
            }
        }
    }
}