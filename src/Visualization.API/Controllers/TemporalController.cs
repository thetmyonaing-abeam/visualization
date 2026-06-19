using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Visualization.API.Data;

namespace Visualization.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TemporalController : ControllerBase
{
    private readonly AppDbContext _context;

    public TemporalController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get timeline data for claims
    /// </summary>
    [HttpGet("claims")]
    public async Task<ActionResult> GetClaimTimeline(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var query = _context.Claims.AsQueryable();

        if (startDate.HasValue)
            query = query.Where(c => c.IncidentDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(c => c.IncidentDate <= endDate.Value);

        var claims = await query
            .OrderBy(c => c.IncidentDate)
            .Select(c => new
            {
                c.Id,
                c.ClaimNumber,
                c.Status,
                c.Type,
                c.Amount,
                c.IncidentDate,
                c.FiledDate,
                c.Description,
                EntityName = c.Entity.Name,
                EntityType = c.Entity.Type,
                AlertCount = c.Alerts.Count
            }).ToListAsync();

        return Ok(claims);
    }

    /// <summary>
    /// Get alert timeline
    /// </summary>
    [HttpGet("alerts")]
    public async Task<ActionResult> GetAlertTimeline(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? severity = null)
    {
        var query = _context.Alerts.AsQueryable();

        if (startDate.HasValue)
            query = query.Where(a => a.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(a => a.CreatedAt <= endDate.Value);
        if (!string.IsNullOrEmpty(severity))
            query = query.Where(a => a.Severity == severity);

        var alerts = await query
            .OrderBy(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                a.AlertType,
                a.Severity,
                a.Description,
                a.CreatedAt,
                a.IsResolved,
                a.ResolvedAt,
                EntityName = a.Entity != null ? a.Entity.Name : null,
                ClaimNumber = a.Claim != null ? a.Claim.ClaimNumber : null
            }).ToListAsync();

        return Ok(alerts);
    }

    /// <summary>
    /// Get activity summary grouped by time period
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult> GetActivitySummary([FromQuery] string period = "month")
    {
        var claims = await _context.Claims.ToListAsync();
        var alerts = await _context.Alerts.ToListAsync();

        object summary = period switch
        {
            "day" => new
            {
                claims = claims.GroupBy(c => c.IncidentDate.Date)
                    .Select(g => new { date = g.Key, count = g.Count(), totalAmount = g.Sum(c => c.Amount) })
                    .OrderBy(x => x.date),
                alerts = alerts.GroupBy(a => a.CreatedAt.Date)
                    .Select(g => new { date = g.Key, count = g.Count() })
                    .OrderBy(x => x.date)
            },
            "week" => new
            {
                claims = claims.GroupBy(c => new { Year = c.IncidentDate.Year, Week = GetWeekOfYear(c.IncidentDate) })
                    .Select(g => new { year = g.Key.Year, week = g.Key.Week, count = g.Count(), totalAmount = g.Sum(c => c.Amount) })
                    .OrderBy(x => x.year).ThenBy(x => x.week),
                alerts = alerts.GroupBy(a => new { Year = a.CreatedAt.Year, Week = GetWeekOfYear(a.CreatedAt) })
                    .Select(g => new { year = g.Key.Year, week = g.Key.Week, count = g.Count() })
                    .OrderBy(x => x.year).ThenBy(x => x.week)
            },
            _ => new
            {
                claims = claims.GroupBy(c => new { c.IncidentDate.Year, c.IncidentDate.Month })
                    .Select(g => new { year = g.Key.Year, month = g.Key.Month, count = g.Count(), totalAmount = g.Sum(c => c.Amount) })
                    .OrderBy(x => x.year).ThenBy(x => x.month),
                alerts = alerts.GroupBy(a => new { a.CreatedAt.Year, a.CreatedAt.Month })
                    .Select(g => new { year = g.Key.Year, month = g.Key.Month, count = g.Count() })
                    .OrderBy(x => x.year).ThenBy(x => x.month)
            } as object
        };

        return Ok(summary);
    }

    private static int GetWeekOfYear(DateTime date)
    {
        return System.Globalization.CultureInfo.CurrentCulture.Calendar
            .GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }
}
