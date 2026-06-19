using Microsoft.AspNetCore.Mvc;
using Visualization.API.Services;

namespace Visualization.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PowerBIController : ControllerBase
{
    private readonly IPowerBIService _powerBIService;

    public PowerBIController(IPowerBIService powerBIService)
    {
        _powerBIService = powerBIService;
    }

    /// <summary>
    /// Get Power BI embed configuration for a specific report
    /// </summary>
    [HttpGet("embed/{reportId}")]
    public async Task<ActionResult<PowerBIEmbedConfig>> GetEmbedConfig(string reportId)
    {
        var config = await _powerBIService.GetEmbedConfigAsync(reportId);
        return Ok(config);
    }

    /// <summary>
    /// Get Power BI settings (report IDs, workspace info)
    /// </summary>
    [HttpGet("settings")]
    public ActionResult GetSettings()
    {
        var settings = _powerBIService.GetSettings();
        return Ok(new
        {
            settings.WorkspaceId,
            settings.GeospatialReportId,
            settings.TemporalReportId
        });
    }

    /// <summary>
    /// Get embed config for the geospatial report
    /// </summary>
    [HttpGet("embed/geospatial")]
    public async Task<ActionResult<PowerBIEmbedConfig>> GetGeospatialEmbed()
    {
        var settings = _powerBIService.GetSettings();
        if (string.IsNullOrEmpty(settings.GeospatialReportId))
            return Ok(new { message = "Geospatial report ID not configured. Set PowerBI:GeospatialReportId in appsettings.json" });

        var config = await _powerBIService.GetEmbedConfigAsync(settings.GeospatialReportId);
        return Ok(config);
    }

    /// <summary>
    /// Get embed config for the temporal report
    /// </summary>
    [HttpGet("embed/temporal")]
    public async Task<ActionResult<PowerBIEmbedConfig>> GetTemporalEmbed()
    {
        var settings = _powerBIService.GetSettings();
        if (string.IsNullOrEmpty(settings.TemporalReportId))
            return Ok(new { message = "Temporal report ID not configured. Set PowerBI:TemporalReportId in appsettings.json" });

        var config = await _powerBIService.GetEmbedConfigAsync(settings.TemporalReportId);
        return Ok(config);
    }
}
