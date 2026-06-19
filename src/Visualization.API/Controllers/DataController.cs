using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Visualization.API.Data;
using Visualization.API.Models;

namespace Visualization.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    private readonly AppDbContext _context;

    public DataController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all entities with optional type filtering
    /// </summary>
    [HttpGet("entities")]
    public async Task<ActionResult<IEnumerable<Entity>>> GetEntities([FromQuery] string? type = null)
    {
        var query = _context.Entities.AsQueryable();
        if (!string.IsNullOrEmpty(type))
            query = query.Where(e => e.Type == type);

        return Ok(await query.ToListAsync());
    }

    /// <summary>
    /// Get all claims with optional filtering
    /// </summary>
    [HttpGet("claims")]
    public async Task<ActionResult<IEnumerable<Claim>>> GetClaims(
        [FromQuery] string? status = null,
        [FromQuery] string? type = null)
    {
        var query = _context.Claims.Include(c => c.Entity).AsQueryable();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status == status);
        if (!string.IsNullOrEmpty(type))
            query = query.Where(c => c.Type == type);

        return Ok(await query.ToListAsync());
    }

    /// <summary>
    /// Get all alerts with optional filtering
    /// </summary>
    [HttpGet("alerts")]
    public async Task<ActionResult<IEnumerable<Alert>>> GetAlerts(
        [FromQuery] string? severity = null,
        [FromQuery] string? alertType = null)
    {
        var query = _context.Alerts
            .Include(a => a.Entity)
            .Include(a => a.Claim)
            .AsQueryable();

        if (!string.IsNullOrEmpty(severity))
            query = query.Where(a => a.Severity == severity);
        if (!string.IsNullOrEmpty(alertType))
            query = query.Where(a => a.AlertType == alertType);

        return Ok(await query.ToListAsync());
    }

    /// <summary>
    /// Seed the MySQL database with sample data
    /// </summary>
    [HttpPost("seed")]
    public async Task<ActionResult> SeedDatabase()
    {
        await SeedData.InitializeAsync(_context);
        return Ok(new { message = "Database seeded successfully" });
    }

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult> GetStats()
    {
        var stats = new
        {
            totalEntities = await _context.Entities.CountAsync(),
            totalClaims = await _context.Claims.CountAsync(),
            totalAlerts = await _context.Alerts.CountAsync(),
            unresolvedAlerts = await _context.Alerts.CountAsync(a => !a.IsResolved),
            totalClaimAmount = await _context.Claims.SumAsync(c => c.Amount),
            claimsByStatus = await _context.Claims.GroupBy(c => c.Status)
                .Select(g => new { status = g.Key, count = g.Count() }).ToListAsync(),
            alertsBySeverity = await _context.Alerts.GroupBy(a => a.Severity)
                .Select(g => new { severity = g.Key, count = g.Count() }).ToListAsync()
        };

        return Ok(stats);
    }
}
