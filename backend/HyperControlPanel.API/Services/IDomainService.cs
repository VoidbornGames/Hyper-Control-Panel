using HyperControlPanel.API.Models;

namespace HyperControlPanel.API.Services
{
    public interface IDomainService
    {
        Task<bool> ConfigureDomainAsync(Guid domainId);
        Task<bool> VerifyDnsAsync(Guid domainId);
        Task<bool> SetupSslCertificateAsync(Guid domainId);
        Task<bool> RemoveSslCertificateAsync(string domainName);
        Task<bool> UpdateDnsRecordAsync(string domainName, string recordType, string value);
        Task<bool> CreateDnsRecordAsync(string domainName, string recordType, string value);
        Task<bool> DeleteDnsRecordAsync(string domainName, string recordType);
        Task<string?> GetDnsVerificationTokenAsync(Guid domainId);
        Task<bool> IsDomainAccessibleAsync(string domainName);
        Task<DateTime?> GetSslExpiryDateAsync(string domainName);
    }
}