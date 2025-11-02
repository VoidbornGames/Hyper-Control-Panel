using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using HyperControlPanel.API.Models;
using System.Text.Json;

namespace HyperControlPanel.API.Services
{
    public class DockerService : IDockerService
    {
        private readonly ILogger<DockerService> _logger;
        private readonly IConfiguration _configuration;
        private readonly DockerClient _dockerClient;

        public DockerService(
            ILogger<DockerService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            var dockerEndpoint = _configuration["Docker:Host"] ?? "unix:///var/run/docker.sock";
            _dockerClient = new DockerClientConfiguration(new Uri(dockerEndpoint)).CreateClient();
        }

        public async Task<(string ContainerName, string ContainerId)> CreateSiteContainer(Site site)
        {
            try
            {
                var containerName = $"site-{site.Id:N}";
                var networkName = _configuration["Docker:Network"] ?? "hypercontrol-sites";

                // Ensure network exists
                await CreateNetworkAsync(networkName);

                var containerConfig = new CreateContainerParameters
                {
                    Name = containerName,
                    Image = GetImageForPlatform(site.Platform),
                    Env = new List<string>
                    {
                        $"SITE_ID={site.Id}",
                        $"DOMAIN={site.Domain}",
                        $"PLATFORM={site.Platform}",
                        $"SITE_PATH=/var/www/html"
                    },
                    HostConfig = new HostConfig
                    {
                        PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            { "80/tcp", new List<PortBinding> { new PortBinding { HostPort = GetFreePort().ToString() } } }
                        },
                        Binds = new List<string>
                        {
                            $"{site.SiteDirectory}:/var/www/html:rw"
                        },
                        Memory = site.StorageLimitGB * 1024L * 1024L * 1024L, // Convert GB to bytes
                        NetworkMode = networkName
                    },
                    NetworkingConfig = new NetworkingConfig
                    {
                        EndpointsConfig = new Dictionary<string, EndpointSettings>
                        {
                            { networkName, new EndpointSettings() }
                        }
                    }
                };

                var response = await _dockerClient.Containers.CreateContainerAsync(containerConfig);

                // Start the container
                await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());

                _logger.LogInformation("Created container {ContainerId} for site {SiteId}", response.ID, site.Id);

                return (containerName, response.ID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating container for site {SiteId}", site.Id);
                throw;
            }
        }

        public async Task<bool> RemoveContainer(string containerId)
        {
            try
            {
                // Stop container first
                await StopContainer(containerId);

                // Remove container
                await _dockerClient.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters
                {
                    Force = true,
                    RemoveVolumes = true
                });

                _logger.LogInformation("Removed container {ContainerId}", containerId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing container {ContainerId}", containerId);
                return false;
            }
        }

        public async Task<bool> StartContainer(string containerId)
        {
            try
            {
                await _dockerClient.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
                _logger.LogInformation("Started container {ContainerId}", containerId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting container {ContainerId}", containerId);
                return false;
            }
        }

        public async Task<bool> StopContainer(string containerId)
        {
            try
            {
                await _dockerClient.Containers.StopContainerAsync(containerId, new ContainerStopParameters
                {
                    WaitBeforeKillSeconds = 10
                });

                _logger.LogInformation("Stopped container {ContainerId}", containerId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping container {ContainerId}", containerId);
                return false;
            }
        }

        public async Task<bool> RestartContainer(string containerId)
        {
            try
            {
                await _dockerClient.Containers.RestartContainerAsync(containerId, new ContainerRestartParameters
                {
                    WaitBeforeKillSeconds = 10
                });

                _logger.LogInformation("Restarted container {ContainerId}", containerId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restarting container {ContainerId}", containerId);
                return false;
            }
        }

        public async Task<string?> GetContainerLogs(string containerId)
        {
            try
            {
                var logsParameters = new ContainerLogsParameters
                {
                    ShowStdout = true,
                    ShowStderr = true,
                    Timestamps = true,
                    Tail = "100" // Last 100 lines
                };

                var stream = await _dockerClient.Containers.GetContainerLogsAsync(containerId, logsParameters);
                using var reader = new StreamReader(stream);
                var logs = await reader.ReadToEndAsync();

                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting logs for container {ContainerId}", containerId);
                return null;
            }
        }

        public async Task<bool> ExecuteCommandAsync(string containerId, string command)
        {
            try
            {
                var execCreateParameters = new ContainerExecCreateParameters
                {
                    Cmd = new List<string> { "/bin/sh", "-c", command },
                    AttachStdout = true,
                    AttachStderr = true
                };

                var execResponse = await _dockerClient.Exec.CreateContainerExecAsync(containerId, execCreateParameters);

                var execStartParameters = new ContainerExecStartParameters();
                await _dockerClient.Exec.StartContainerExecAsync(execResponse.ID, execStartParameters);

                _logger.LogInformation("Executed command '{Command}' in container {ContainerId}", command, containerId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing command '{Command}' in container {ContainerId}", command, containerId);
                return false;
            }
        }

        public async Task<ContainerInfo?> GetContainerInfo(string containerId)
        {
            try
            {
                var container = await _dockerClient.Containers.InspectContainerAsync(containerId);

                var stats = await _dockerClient.Containers.GetContainerStatsAsync(containerId, new ContainerStatsParameters { Stream = false });
                var statsJson = await new StreamReader(stats).ReadToEndAsync();
                var resourceStats = JsonSerializer.Deserialize<ResourceStats>(statsJson);

                return new ContainerInfo
                {
                    Id = container.ID,
                    Name = container.Name.TrimStart('/'),
                    Status = container.State.Status,
                    Image = container.Config.Image,
                    CreatedAt = container.Created,
                    ResourceUsage = new ResourceUsage
                    {
                        CpuUsage = resourceStats?.CPUStats?.CPUUsage?.TotalUsage ?? 0,
                        MemoryUsage = resourceStats?.MemoryStats?.Usage ?? 0,
                        MemoryLimit = resourceStats?.MemoryStats?.Limit ?? 0,
                        MemoryUsagePercent = resourceStats?.MemoryStats?.Limit > 0
                            ? (double)(resourceStats?.MemoryStats?.Usage ?? 0) / resourceStats.MemoryStats.Limit * 100
                            : 0
                    },
                    Ports = container.NetworkSettings.Ports.SelectMany(p => p.Value?.Select(pb => $"{pb.HostPort}{p.Key}") ?? Array.Empty<string>()).ToList(),
                    Networks = container.NetworkSettings.Networks.Keys.ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting container info for {ContainerId}", containerId);
                return null;
            }
        }

        public async Task<List<ContainerInfo>> GetSiteContainersAsync(string userId)
        {
            var containers = new List<ContainerInfo>();

            try
            {
                var siteContainers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters
                {
                    All = true
                });

                foreach (var container in siteContainers.Where(c => c.Names.Any(n => n.StartsWith("/site-"))))
                {
                    var info = await GetContainerInfo(container.ID);
                    if (info != null)
                    {
                        containers.Add(info);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting site containers for user {UserId}", userId);
            }

            return containers;
        }

        public async Task<bool> UpdateContainerResourcesAsync(string containerId, ResourceLimits limits)
        {
            try
            {
                var updateParameters = new ContainerUpdateParameters
                {
                    Memory = limits.MemoryLimit,
                    CpuQuota = limits.CpuLimit.HasValue ? (long)(limits.CpuLimit.Value * 100000) : null
                };

                await _dockerClient.Containers.UpdateContainerAsync(containerId, updateParameters);

                _logger.LogInformation("Updated resources for container {ContainerId}", containerId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating resources for container {ContainerId}", containerId);
                return false;
            }
        }

        public async Task<bool> CreateNetworkAsync(string networkName)
        {
            try
            {
                // Check if network already exists
                var networks = await _dockerClient.Networks.ListNetworksAsync();
                if (networks.Any(n => n.Name == networkName))
                {
                    return true;
                }

                var createParameters = new NetworksCreateParameters
                {
                    Name = networkName,
                    Driver = "bridge",
                    Internal = false,
                    CheckDuplicate = true
                };

                await _dockerClient.Networks.CreateNetworkAsync(createParameters);
                _logger.LogInformation("Created network {NetworkName}", networkName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating network {NetworkName}", networkName);
                return false;
            }
        }

        public async Task<bool> ConnectContainerToNetwork(string containerId, string networkName)
        {
            try
            {
                var parameters = new NetworkConnectParameters
                {
                    Container = containerId
                };

                await _dockerClient.Networks.ConnectNetworkAsync(networkName, parameters);
                _logger.LogInformation("Connected container {ContainerId} to network {NetworkName}", containerId, networkName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting container {ContainerId} to network {NetworkName}", containerId, networkName);
                return false;
            }
        }

        private string GetImageForPlatform(string platform)
        {
            return platform.ToLower() switch
            {
                "wordpress" => "wordpress:latest",
                "nginx" => "nginx:alpine",
                "apache" => "httpd:latest",
                "php" => "php:8-apache",
                "node" => "node:18-alpine",
                "hugo" => "klakegg/hugo:ext-alpine",
                "jekyll" => "jekyll/jekyll:latest",
                _ => "nginx:alpine"
            };
        }

        private int GetFreePort()
        {
            using var socket = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            socket.Start();
            var port = ((System.Net.IPEndPoint)socket.LocalEndpoint).Port;
            socket.Stop();
            return port;
        }

        // Helper classes for deserializing Docker stats
        private class ResourceStats
        {
            public CPUStats? CPUStats { get; set; }
            public MemoryStats? MemoryStats { get; set; }
        }

        private class CPUStats
        {
            public CPUUsage? CPUUsage { get; set; }
        }

        private class CPUUsage
        {
            public long TotalUsage { get; set; }
        }

        private class MemoryStats
        {
            public long Usage { get; set; }
            public long Limit { get; set; }
        }
    }
}