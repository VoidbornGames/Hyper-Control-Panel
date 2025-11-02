using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HyperControlPanel.API.Data;
using HyperControlPanel.API.DTOs;
using HyperControlPanel.API.Services;
using AutoMapper;

namespace HyperControlPanel.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SitesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISiteService _siteService;
        private readonly IDomainService _domainService;
        private readonly IDatabaseService _databaseService;
        private readonly IMapper _mapper;
        private readonly ILogger<SitesController> _logger;

        public SitesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ISiteService siteService,
            IDomainService domainService,
            IDatabaseService databaseService,
            IMapper mapper,
            ILogger<SitesController> logger)
        {
            _context = context;
            _userManager = userManager;
            _siteService = siteService;
            _domainService = domainService;
            _databaseService = databaseService;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Get all sites for the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SiteListDto>>> GetSites()
        {
            var userId = _userManager.GetUserId(User);
            var sites = await _context.Sites
                .Where(s => s.UserId == userId)
                .Include(s => s.Domains)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var siteDtos = _mapper.Map<List<SiteListDto>>(sites);
            return Ok(siteDtos);
        }

        /// <summary>
        /// Get a specific site by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<SiteDto>> GetSite(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            var site = await _context.Sites
                .Include(s => s.Domains)
                .Include(s => s.Databases)
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (site == null)
            {
                return NotFound();
            }

            var siteDto = _mapper.Map<SiteDto>(site);
            return Ok(siteDto);
        }

        /// <summary>
        /// Create a new site
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<SiteDto>> CreateSite([FromBody] CreateSiteRequestDto request)
        {
            var userId = _userManager.GetUserId(User);

            // Check if domain already exists
            var existingDomain = await _context.Sites
                .AnyAsync(s => s.Domain == request.Domain || s.Domains.Any(d => d.DomainName == request.Domain));

            if (existingDomain)
            {
                return BadRequest(new { error = "Domain already exists" });
            }

            // Check if user has reached their site limit (you might want to add this to user settings)
            var userSiteCount = await _context.Sites.CountAsync(s => s.UserId == userId);
            if (userSiteCount >= 50) // Example limit
            {
                return BadRequest(new { error = "Maximum site limit reached" });
            }

            try
            {
                var site = new Site
                {
                    Name = request.Name,
                    Description = request.Description,
                    Domain = request.Domain,
                    UserId = userId,
                    Platform = request.Platform,
                    Template = request.Template,
                    StorageLimitGB = request.StorageLimitGB,
                    Status = "creating"
                };

                _context.Sites.Add(site);
                await _context.SaveChangesAsync();

                // Add primary domain
                var primaryDomain = new Domain
                {
                    SiteId = site.Id,
                    DomainName = request.Domain,
                    Type = "subdomain",
                    IsPrimary = true
                };

                _context.Domains.Add(primaryDomain);
                await _context.SaveChangesAsync();

                // Start site creation process
                _ = Task.Run(async () => await _siteService.CreateSiteAsync(site.Id));

                var siteDto = _mapper.Map<SiteDto>(site);
                return CreatedAtAction(nameof(GetSite), new { id = site.Id }, siteDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating site");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Update an existing site
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<SiteDto>> UpdateSite(Guid id, [FromBody] UpdateSiteRequestDto request)
        {
            var userId = _userManager.GetUserId(User);
            var site = await _context.Sites.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (site == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(request.Name))
                site.Name = request.Name;

            if (request.Description != null)
                site.Description = request.Description;

            if (request.StorageLimitGB.HasValue)
                site.StorageLimitGB = request.StorageLimitGB.Value;

            site.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                var siteDto = _mapper.Map<SiteDto>(site);
                return Ok(siteDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating site {SiteId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Delete a site
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteSite(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            var site = await _context.Sites
                .Include(s => s.Domains)
                .Include(s => s.Databases)
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (site == null)
            {
                return NotFound();
            }

            try
            {
                // Start site deletion process
                _ = Task.Run(async () => await _siteService.DeleteSiteAsync(site.Id));

                // Mark as deleting in database
                site.Status = "deleting";
                site.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Site deletion started" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting site {SiteId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get site statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<SiteStatsDto>> GetSiteStats()
        {
            var userId = _userManager.GetUserId(User);

            var sites = await _context.Sites
                .Include(s => s.Domains)
                .Include(s => s.Databases)
                .Where(s => s.UserId == userId)
                .ToListAsync();

            var stats = new SiteStatsDto
            {
                TotalSites = sites.Count,
                ActiveSites = sites.Count(s => s.Status == "active"),
                SuspendedSites = sites.Count(s => s.Status == "suspended"),
                TotalStorageUsedMB = sites.Sum(s => s.StorageUsedMB),
                TotalDomains = sites.SelectMany(s => s.Domains).Count(),
                DomainsWithSsl = sites.SelectMany(s => s.Domains).Count(d => d.SslEnabled),
                TotalDatabases = sites.SelectMany(s => s.Databases).Count(),
                LastActivity = sites.OrderByDescending(s => s.UpdatedAt).FirstOrDefault()?.UpdatedAt ?? DateTime.MinValue
            };

            return Ok(stats);
        }

        /// <summary>
        /// Create a backup of a site
        /// </summary>
        [HttpPost("{id}/backup")]
        public async Task<ActionResult<BackupDto>> CreateBackup(Guid id, [FromBody] CreateBackupRequestDto request)
        {
            var userId = _userManager.GetUserId(User);
            var site = await _context.Sites.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (site == null)
            {
                return NotFound();
            }

            try
            {
                var backupId = await _siteService.CreateBackupAsync(site.Id, request);
                var backup = await _context.SiteBackups.FindAsync(backupId);
                var backupDto = _mapper.Map<BackupDto>(backup);

                return Ok(backupDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating backup for site {SiteId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get all backups for a site
        /// </summary>
        [HttpGet("{id}/backups")]
        public async Task<ActionResult<IEnumerable<BackupDto>>> GetSiteBackups(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            var siteExists = await _context.Sites.AnyAsync(s => s.Id == id && s.UserId == userId);

            if (!siteExists)
            {
                return NotFound();
            }

            var backups = await _context.SiteBackups
                .Where(b => b.SiteId == id)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var backupDtos = _mapper.Map<List<BackupDto>>(backups);
            return Ok(backupDtos);
        }

        /// <summary>
        /// Clone an existing site
        /// </summary>
        [HttpPost("{id}/clone")]
        public async Task<ActionResult<SiteDto>> CloneSite(Guid id, [FromBody] CloneSiteRequestDto request)
        {
            var userId = _userManager.GetUserId(User);
            var site = await _context.Sites.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (site == null)
            {
                return NotFound();
            }

            try
            {
                var newSiteId = await _siteService.CloneSiteAsync(site.Id, request);
                var newSite = await _context.Sites.FindAsync(newSiteId);
                var siteDto = _mapper.Map<SiteDto>(newSite);

                return CreatedAtAction(nameof(GetSite), new { id = newSiteId }, siteDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cloning site {SiteId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get deployment history for a site
        /// </summary>
        [HttpGet("{id}/deployments")]
        public async Task<ActionResult<IEnumerable<DeploymentDto>>> GetSiteDeployments(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            var siteExists = await _context.Sites.AnyAsync(s => s.Id == id && s.UserId == userId);

            if (!siteExists)
            {
                return NotFound();
            }

            var deployments = await _context.Deployments
                .Where(d => d.SiteId == id)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            var deploymentDtos = _mapper.Map<List<DeploymentDto>>(deployments);
            return Ok(deploymentDtos);
        }

        /// <summary>
        /// Restart site services
        /// </summary>
        [HttpPost("{id}/restart")]
        public async Task<ActionResult> RestartSite(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            var site = await _context.Sites.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (site == null)
            {
                return NotFound();
            }

            try
            {
                await _siteService.RestartSiteAsync(site.Id);
                return Ok(new { message = "Site restart initiated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restarting site {SiteId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }

    // Additional DTO for cloning
    public class CloneSiteRequestDto
    {
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Domain { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public bool CloneDatabase { get; set; } = true;
        public bool CloneFiles { get; set; } = true;
    }
}