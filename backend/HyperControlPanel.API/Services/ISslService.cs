namespace HyperControlPanel.API.Services
{
    public interface ISslService
    {
        Task<bool> RequestSslCertificateAsync(string domainName, string email);
        Task<bool> RenewSslCertificateAsync(string domainName);
        Task<bool> RevokeSslCertificateAsync(string domainName);
        Task<DateTime?> GetCertificateExpiryAsync(string domainName);
        Task<bool> InstallCertificateAsync(string domainName, string certificatePath, string keyPath);
        Task<bool> CreateWildcardCertificateAsync(string baseDomain, string email);
        Task<bool> IsCertificateValidAsync(string domainName);
        Task<string?> GetCertificateDetailsAsync(string domainName);
        Task<List<string>> GetExpiringCertificatesAsync(int daysThreshold = 30);
    }
}