using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Visualization.API.Data;

namespace Visualization.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GeospatialController : ControllerBase
{
    private readonly AppDbContext _context;

    public GeospatialController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all entities with location data for map visualization
    /// </summary>
    [HttpGet("entities")]
    public async Task<ActionResult> GetEntityLocations([FromQuery] string? type = null)
    {
        var query = _context.Entities
            .Where(e => e.Latitude != null && e.Longitude != null);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(e => e.Type == type);

        var entities = await query.Select(e => new
        {
            e.Id,
            e.Name,
            e.Type,
            e.Latitude,
            e.Longitude,
            e.Address,
            ClaimCount = e.Claims.Count,
            AlertCount = e.Alerts.Count
        }).ToListAsync();

        // Return as GeoJSON for Mapbox
        var geoJson = new
        {
            type = "FeatureCollection",
            features = entities.Select(e => new
            {
                type = "Feature",
                geometry = new
                {
                    type = "Point",
                    coordinates = new[] { e.Longitude!.Value, e.Latitude!.Value }
                },
                properties = new
                {
                    id = e.Id,
                    name = e.Name,
                    entityType = e.Type,
                    address = e.Address,
                    claimCount = e.ClaimCount,
                    alertCount = e.AlertCount
                }
            })
        };

        return Ok(geoJson);
    }

    /// <summary>
    /// Get claim incident locations for map visualization
    /// </summary>
    [HttpGet("claims")]
    public async Task<ActionResult> GetClaimLocations(
        [FromQuery] string? status = null,
        [FromQuery] string? type = null)
    {
        var query = _context.Claims
            .Where(c => c.IncidentLatitude != null && c.IncidentLongitude != null);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status == status);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(c => c.Type == type);

        var claims = await query.Select(c => new
        {
            c.Id,
            c.ClaimNumber,
            c.Status,
            c.Type,
            c.Amount,
            c.IncidentDate,
            c.IncidentLatitude,
            c.IncidentLongitude,
            c.IncidentLocation,
            c.Description,
            EntityName = c.Entity.Name,
            AlertCount = c.Alerts.Count
        }).ToListAsync();

        var geoJson = new
        {
            type = "FeatureCollection",
            features = claims.Select(c => new
            {
                type = "Feature",
                geometry = new
                {
                    type = "Point",
                    coordinates = new[] { c.IncidentLongitude!.Value, c.IncidentLatitude!.Value }
                },
                properties = new
                {
                    id = c.Id,
                    claimNumber = c.ClaimNumber,
                    status = c.Status,
                    claimType = c.Type,
                    amount = c.Amount,
                    incidentDate = c.IncidentDate,
                    location = c.IncidentLocation,
                    description = c.Description,
                    entityName = c.EntityName,
                    alertCount = c.AlertCount
                }
            })
        };

        return Ok(geoJson);
    }

    /// <summary>
    /// Get heatmap data for claim density
    /// </summary>
    [HttpGet("heatmap")]
    public async Task<ActionResult> GetHeatmapData()
    {
        var claims = await _context.Claims
            .Where(c => c.IncidentLatitude != null && c.IncidentLongitude != null)
            .Select(c => new
            {
                c.IncidentLatitude,
                c.IncidentLongitude,
                weight = (double)c.Amount / 10000.0
            }).ToListAsync();

        return Ok(claims);
    }

    /// <summary>
    /// Get Mapbox configuration
    /// </summary>
    [HttpGet("config")]
    public ActionResult GetMapConfig([FromServices] IConfiguration configuration)
    {
        return Ok(new
        {
            accessToken = configuration["Mapbox:AccessToken"] ?? "YOUR_MAPBOX_TOKEN",
            style = configuration["Mapbox:Style"] ?? "mapbox://styles/mapbox/dark-v11",
            center = new[] { -73.98, 40.73 },
            zoom = 11
        });
    }
}
