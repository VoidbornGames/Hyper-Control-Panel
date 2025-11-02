using HyperControlPanel.API.Models;

namespace HyperControlPanel.API.Services
{
    public interface IDockerService
    {
        Task<(string ContainerName, string ContainerId)> CreateSiteContainer(Site site);
        Task<bool> RemoveContainer(string containerId);
        Task<bool> StartContainer(string containerId);
        Task<bool> StopContainer(string containerId);
        Task<bool> RestartContainer(string containerId);
        Task<string?> GetContainerLogs(string containerId);
        Task<bool> ExecuteCommandAsync(string containerId, string command);
        Task<ContainerInfo?> GetContainerInfo(string containerId);
        Task<List<ContainerInfo>> GetSiteContainersAsync(string userId);
        Task<bool> UpdateContainerResourcesAsync(string containerId, ResourceLimits limits);
        Task<bool> CreateNetworkAsync(string networkName);
        Task<bool> ConnectContainerToNetwork(string containerId, string networkName);
    }

    public class ContainerInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public ResourceUsage ResourceUsage { get; set; } = new ResourceUsage();
        public List<string> Ports { get; set; } = new List<string>();
        public List<string> Networks { get; set; } = new List<string>();
    }

    public class ResourceUsage
    {
        public double CpuUsage { get; set; }
        public long MemoryUsage { get; set; }
        public long MemoryLimit { get; set; }
        public double MemoryUsagePercent { get; set; }
        public long StorageUsed { get; set; }
        public long StorageLimit { get; set; }
        public double StorageUsagePercent { get; set; }
    }

    public class ResourceLimits
    {
        public double? CpuLimit { get; set; }
        public long? MemoryLimit { get; set; }
        public long? StorageLimit { get; set; }
    }
}