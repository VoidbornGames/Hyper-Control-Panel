using HyperControlPanel.API.DTOs;
using HyperControlPanel.API.Models;

namespace HyperControlPanel.API.Services
{
    public interface ITemplateService
    {
        Task<IEnumerable<Template>> GetTemplatesAsync(TemplateFilterDto filter);
        Task<Template?> GetTemplateAsync(Guid templateId);
        Task<bool> InstallTemplateAsync(Guid siteId, Guid templateId);
        Task<bool> UninstallTemplateAsync(Guid siteId);
        Task<bool> ValidateTemplateAsync(string templatePath);
        Task<Template?> CreateTemplateAsync(CreateTemplateDto templateDto);
        Task<bool> UpdateTemplateAsync(Guid templateId, UpdateTemplateDto templateDto);
        Task<bool> DeleteTemplateAsync(Guid templateId);
        Task<string?> GetTemplatePreviewAsync(Guid templateId);
        Task<List<string>> GetAvailablePlatformsAsync();
        Task<List<string>> GetTemplateCategoriesAsync(string platform);
    }

    public class CreateTemplateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Platform { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string TemplatePath { get; set; } = string.Empty;
        public string? ScreenshotUrl { get; set; }
        public string? PreviewUrl { get; set; }
        public string? ManifestJson { get; set; }
    }

    public class UpdateTemplateDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ScreenshotUrl { get; set; }
        public string? PreviewUrl { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsFeatured { get; set; }
        public int? SortOrder { get; set; }
    }
}