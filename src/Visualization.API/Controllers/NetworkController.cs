using Microsoft.AspNetCore.Mvc;
using Visualization.API.Models;
using Visualization.API.Services;

namespace Visualization.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NetworkController : ControllerBase
{
    private readonly INeo4jService _neo4jService;

    public NetworkController(INeo4jService neo4jService)
    {
        _neo4jService = neo4jService;
    }

    /// <summary>
    /// Get the full network graph with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<NetworkGraph>> GetNetwork([FromQuery] NetworkFilter? filter)
    {
        var graph = await _neo4jService.GetFullNetworkAsync(filter);
        return Ok(graph);
    }

    /// <summary>
    /// Get network centered on a specific entity with configurable depth
    /// </summary>
    [HttpGet("entity/{entityId}")]
    public async Task<ActionResult<NetworkGraph>> GetEntityNetwork(string entityId, [FromQuery] int depth = 2)
    {
        var graph = await _neo4jService.GetEntityNetworkAsync(entityId, depth);
        return Ok(graph);
    }

    /// <summary>
    /// Find the shortest path between two entities
    /// </summary>
    [HttpGet("path")]
    public async Task<ActionResult<PathResult>> FindPath([FromQuery] string from, [FromQuery] string to)
    {
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
            return BadRequest("Both 'from' and 'to' entity IDs are required");

        var path = await _neo4jService.FindShortestPathAsync(from, to);
        return Ok(path);
    }

    /// <summary>
    /// Get network metrics (centrality indicators, density, etc.)
    /// </summary>
    [HttpGet("metrics")]
    public async Task<ActionResult<NetworkMetrics>> GetMetrics()
    {
        var metrics = await _neo4jService.GetNetworkMetricsAsync();
        return Ok(metrics);
    }

    /// <summary>
    /// Get time-based network analysis for a date range
    /// </summary>
    [HttpGet("timeline")]
    public async Task<ActionResult<NetworkGraph>> GetTimelineNetwork(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var graph = await _neo4jService.GetTimeBasedNetworkAsync(startDate, endDate);
        return Ok(graph);
    }

    /// <summary>
    /// Seed the Neo4j database with sample data
    /// </summary>
    [HttpPost("seed")]
    public async Task<ActionResult> SeedData()
    {
        await _neo4jService.SeedGraphDataAsync();
        return Ok(new { message = "Neo4j graph data seeded successfully" });
    }
}
