using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HyperControlPanel.API.Data;
using HyperControlPanel.API.Models;

namespace HyperControlPanel.API.Services
{
    public class DomainService : IDomainService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISslService _sslService;
        private readonly ILogger<DomainService> _logger;

        public DomainService(
            ApplicationDbContext context,
            ISslService sslService,
            ILogger<DomainService> logger)
        {
            _context = context;
            _sslService = sslService;
            _logger = logger;
        }

        public async Task<bool> ConfigureDomainAsync(Guid domainId)
        {
            try
            {
                var domain = await _context.Domains.FindAsync(domainId);
                if (domain == null)
                {
                    return false;
                }

                // Generate DNS verification token if subdomain
                if (domain.Type == "subdomain")
                {
                    domain.DnsVerificationToken = GenerateVerificationToken();
                    domain.DnsVerified = true; // Auto-verify subdomains
                }
                else
                {
                    // For custom domains, generate token for DNS verification
                    domain.DnsVerificationToken = GenerateVerificationToken();
                }

                await _context.SaveChangesAsync();

                // Attempt to set up SSL certificate
                await SetupSslCertificateAsync(domainId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring domain {DomainId}", domainId);
                return false;
            }
        }

        public async Task<bool> VerifyDnsAsync(Guid domainId)
        {
            try
            {
                var domain = await _context.Domains.FindAsync(domainId);
                if (domain == null)
                {
                    return false;
                }

                if (domain.Type == "subdomain")
                {
                    domain.DnsVerified = true;
                }
                else
                {
                    // For custom domains, verify TXT record exists
                    var isVerified = await CheckDnsVerification(domain.DomainName, domain.DnsVerificationToken!);
                    domain.DnsVerified = isVerified;
                }

                await _context.SaveChangesAsync();
                return domain.DnsVerified;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying DNS for domain {DomainId}", domainId);
                return false;
            }
        }

        public async Task<bool> SetupSslCertificateAsync(Guid domainId)
        {
            try
            {
                var domain = await _context.Domains.FindAsync(domainId);
                if (domain == null || !domain.DnsVerified)
                {
                    return false;
                }

                var success = await _sslService.RequestSslCertificateAsync(domain.DomainName, "admin@" + domain.DomainName);
                if (success)
                {
                    domain.SslEnabled = true;
                    domain.SslExpiresAt = await _sslService.GetCertificateExpiryAsync(domain.DomainName);
                    await _context.SaveChangesAsync();
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up SSL certificate for domain {DomainId}", domainId);
                return false;
            }
        }

        public async Task<bool> RemoveSslCertificateAsync(string domainName)
        {
            try
            {
                return await _sslService.RevokeSslCertificateAsync(domainName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing SSL certificate for domain {DomainName}", domainName);
                return false;
            }
        }

        public async Task<bool> UpdateDnsRecordAsync(string domainName, string recordType, string value)
        {
            try
            {
                // This would integrate with DNS provider API
                // For now, just log the operation
                _logger.LogInformation("Updating DNS record {RecordType} for {DomainName} to {Value}", recordType, domainName, value);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating DNS record for domain {DomainName}", domainName);
                return false;
            }
        }

        public async Task<bool> CreateDnsRecordAsync(string domainName, string recordType, string value)
        {
            try
            {
                _logger.LogInformation("Creating DNS record {RecordType} for {DomainName} with value {Value}", recordType, domainName, value);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating DNS record for domain {DomainName}", domainName);
                return false;
            }
        }

        public async Task<bool> DeleteDnsRecordAsync(string domainName, string recordType)
        {
            try
            {
                _logger.LogInformation("Deleting DNS record {RecordType} for {DomainName}", recordType, domainName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting DNS record for domain {DomainName}", domainName);
                return false;
            }
        }

        public async Task<string?> GetDnsVerificationTokenAsync(Guid domainId)
        {
            var domain = await _context.Domains.FindAsync(domainId);
            return domain?.DnsVerificationToken;
        }

        public async Task<bool> IsDomainAccessibleAsync(string domainName)
        {
            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync($"http://{domainName}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<DateTime?> GetSslExpiryDateAsync(string domainName)
        {
            return await _sslService.GetCertificateExpiryAsync(domainName);
        }

        private string GenerateVerificationToken()
        {
            return Guid.NewGuid().ToString("N")[..16].ToUpper();
        }

        private async Task<bool> CheckDnsVerification(string domainName, string token)
        {
            try
            {
                // This would check for TXT record with the verification token
                // For now, return true for demo purposes
                await Task.Delay(100); // Simulate DNS lookup
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}