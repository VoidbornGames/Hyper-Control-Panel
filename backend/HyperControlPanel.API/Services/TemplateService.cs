using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HyperControlPanel.API.Data;
using HyperControlPanel.API.DTOs;
using HyperControlPanel.API.Models;

namespace HyperControlPanel.API.Services
{
    public class TemplateService : ITemplateService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TemplateService> _logger;

        public TemplateService(
            ApplicationDbContext context,
            ILogger<TemplateService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Template>> GetTemplatesAsync(TemplateFilterDto filter)
        {
            try
            {
                var query = _context.Templates.Where(t => t.IsActive);

                if (!string.IsNullOrEmpty(filter.Platform))
                {
                    query = query.Where(t => t.Platform == filter.Platform);
                }

                if (!string.IsNullOrEmpty(filter.Category))
                {
                    query = query.Where(t => t.Category == filter.Category);
                }

                if (filter.FeaturedOnly)
                {
                    query = query.Where(t => t.IsFeatured);
                }

                return await query
                    .OrderBy(t => t.SortOrder)
                    .ThenBy(t => t.Name)
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting templates");
                return Enumerable.Empty<Template>();
            }
        }

        public async Task<Template?> GetTemplateAsync(Guid templateId)
        {
            try
            {
                return await _context.Templates.FindAsync(templateId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template {TemplateId}", templateId);
                return null;
            }
        }

        public async Task<bool> InstallTemplateAsync(Guid siteId, Guid templateId)
        {
            try
            {
                var site = await _context.Sites.FindAsync(siteId);
                var template = await _context.Templates.FindAsync(templateId);

                if (site == null || template == null)
                {
                    return false;
                }

                // Update site with template information
                site.Template = template.Name;
                site.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Installed template {TemplateName} on site {SiteId}", template.Name, siteId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error installing template {TemplateId} on site {SiteId}", templateId, siteId);
                return false;
            }
        }

        public async Task<bool> UninstallTemplateAsync(Guid siteId)
        {
            try
            {
                var site = await _context.Sites.FindAsync(siteId);
                if (site == null)
                {
                    return false;
                }

                site.Template = "default";
                site.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Uninstalled template from site {SiteId}", siteId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uninstalling template from site {SiteId}", siteId);
                return false;
            }
        }

        public async Task<bool> ValidateTemplateAsync(string templatePath)
        {
            try
            {
                if (!Directory.Exists(templatePath))
                {
                    return false;
                }

                // Check for required files
                var manifestPath = Path.Combine(templatePath, "manifest.json");
                if (!File.Exists(manifestPath))
                {
                    return false;
                }

                // Validate manifest JSON
                var manifestJson = await File.ReadAllTextAsync(manifestPath);
                var manifest = System.Text.Json.JsonSerializer.Deserialize<TemplateManifest>(manifestJson);

                return manifest != null && !string.IsNullOrEmpty(manifest.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating template at {TemplatePath}", templatePath);
                return false;
            }
        }

        public async Task<Template?> CreateTemplateAsync(CreateTemplateDto templateDto)
        {
            try
            {
                var template = new Template
                {
                    Name = templateDto.Name,
                    Description = templateDto.Description,
                    Platform = templateDto.Platform,
                    Category = templateDto.Category,
                    TemplatePath = templateDto.TemplatePath,
                    ScreenshotUrl = templateDto.ScreenshotUrl,
                    PreviewUrl = templateDto.PreviewUrl,
                    ManifestJson = templateDto.ManifestJson,
                    IsActive = true,
                    IsFeatured = false,
                    SortOrder = 0
                };

                _context.Templates.Add(template);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created template {TemplateName}", template.Name);
                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating template {TemplateName}", templateDto.Name);
                return null;
            }
        }

        public async Task<bool> UpdateTemplateAsync(Guid templateId, UpdateTemplateDto templateDto)
        {
            try
            {
                var template = await _context.Templates.FindAsync(templateId);
                if (template == null)
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(templateDto.Name))
                    template.Name = templateDto.Name;

                if (templateDto.Description != null)
                    template.Description = templateDto.Description;

                if (templateDto.ScreenshotUrl != null)
                    template.ScreenshotUrl = templateDto.ScreenshotUrl;

                if (templateDto.PreviewUrl != null)
                    template.PreviewUrl = templateDto.PreviewUrl;

                if (templateDto.IsActive.HasValue)
                    template.IsActive = templateDto.IsActive.Value;

                if (templateDto.IsFeatured.HasValue)
                    template.IsFeatured = templateDto.IsFeatured.Value;

                if (templateDto.SortOrder.HasValue)
                    template.SortOrder = templateDto.SortOrder.Value;

                template.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated template {TemplateId}", templateId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating template {TemplateId}", templateId);
                return false;
            }
        }

        public async Task<bool> DeleteTemplateAsync(Guid templateId)
        {
            try
            {
                var template = await _context.Templates.FindAsync(templateId);
                if (template == null)
                {
                    return false;
                }

                _context.Templates.Remove(template);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted template {TemplateId}", templateId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting template {TemplateId}", templateId);
                return false;
            }
        }

        public async Task<string?> GetTemplatePreviewAsync(Guid templateId)
        {
            try
            {
                var template = await _context.Templates.FindAsync(templateId);
                if (template == null)
                {
                    return null;
                }

                var previewPath = Path.Combine(template.TemplatePath, "preview.html");
                if (File.Exists(previewPath))
                {
                    return await File.ReadAllTextAsync(previewPath);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template preview for {TemplateId}", templateId);
                return null;
            }
        }

        public async Task<List<string>> GetAvailablePlatformsAsync()
        {
            try
            {
                return await _context.Templates
                    .Where(t => t.IsActive)
                    .Select(t => t.Platform)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available platforms");
                return new List<string>();
            }
        }

        public async Task<List<string>> GetTemplateCategoriesAsync(string platform)
        {
            try
            {
                return await _context.Templates
                    .Where(t => t.IsActive && t.Platform == platform)
                    .Select(t => t.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template categories for platform {Platform}", platform);
                return new List<string>();
            }
        }
    }

    public class TemplateManifest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Version { get; set; } = "1.0.0";
        public string Platform { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
        public string? Author { get; set; }
        public string? Homepage { get; set; }
        public Dictionary<string, object>? Variables { get; set; }
        public List<string>? Dependencies { get; set; }
    }
}