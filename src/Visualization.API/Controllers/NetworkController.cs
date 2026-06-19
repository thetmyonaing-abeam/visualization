using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Visualization.API.Data;
using Visualization.API.Models;

namespace Visualization.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NetworkController : ControllerBase
{
    private readonly AppDbContext _context;

    public NetworkController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get the full network graph with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<NetworkGraph>> GetNetwork([FromQuery] NetworkFilter? filter)
    {
        var graph = new NetworkGraph();

        var entityQuery = _context.Entities.AsQueryable();
        if (filter?.EntityTypes?.Any() == true)
        {
            entityQuery = entityQuery.Where(e => filter.EntityTypes.Contains(e.Type));
        }
        if (filter?.StartDate != null)
        {
            entityQuery = entityQuery.Where(e => e.CreatedAt >= filter.StartDate);
        }
        if (filter?.EndDate != null)
        {
            entityQuery = entityQuery.Where(e => e.CreatedAt <= filter.EndDate);
        }

        var entities = await entityQuery.ToListAsync();
        var entityIds = entities.Select(e => e.Id).ToHashSet();

        foreach (var entity in entities)
        {
            graph.Nodes.Add(new NetworkNode
            {
                Id = entity.Id.ToString(),
                Label = entity.Name,
                Group = entity.Type,
                Title = $"{entity.Name} ({entity.Type})",
                Properties = new Dictionary<string, object>
                {
                    ["entityId"] = entity.Type.Substring(0, 1) + entity.Id,
                    ["name"] = entity.Name,
                    ["type"] = entity.Type,
                    ["createdAt"] = entity.CreatedAt.ToString("yyyy-MM-dd")
                }
            });
        }

        var relQuery = _context.Relationships
            .Where(r => entityIds.Contains(r.FromEntityId) && entityIds.Contains(r.ToEntityId));

        if (filter?.RelationshipTypes?.Any() == true)
        {
            relQuery = relQuery.Where(r => filter.RelationshipTypes.Contains(r.Type));
        }
        if (filter?.StartDate != null)
        {
            relQuery = relQuery.Where(r => r.CreatedAt >= filter.StartDate);
        }
        if (filter?.EndDate != null)
        {
            relQuery = relQuery.Where(r => r.CreatedAt <= filter.EndDate);
        }

        var relationships = await relQuery.ToListAsync();

        foreach (var rel in relationships)
        {
            graph.Edges.Add(new NetworkEdge
            {
                From = rel.FromEntityId.ToString(),
                To = rel.ToEntityId.ToString(),
                Label = rel.Type,
                RelationshipType = rel.Type,
                Properties = new Dictionary<string, object>
                {
                    ["date"] = rel.CreatedAt.ToString("yyyy-MM-dd")
                }
            });
        }

        return Ok(graph);
    }

    /// <summary>
    /// Get network centered on a specific entity with configurable depth
    /// </summary>
    [HttpGet("entity/{entityId}")]
    public async Task<ActionResult<NetworkGraph>> GetEntityNetwork(string entityId, [FromQuery] int depth = 2)
    {
        var graph = new NetworkGraph();
        var visitedIds = new HashSet<int>();
        var currentIds = new HashSet<int>();

        // Find the starting entity by matching the entity ID pattern (e.g., "C1" -> Claimant #1)
        var startEntity = await FindEntityByIdAsync(entityId);
        if (startEntity == null)
            return Ok(graph);

        currentIds.Add(startEntity.Id);
        visitedIds.Add(startEntity.Id);

        // BFS traversal up to the specified depth
        for (int d = 0; d < depth && currentIds.Any(); d++)
        {
            var rels = await _context.Relationships
                .Where(r => currentIds.Contains(r.FromEntityId) || currentIds.Contains(r.ToEntityId))
                .Include(r => r.FromEntity)
                .Include(r => r.ToEntity)
                .ToListAsync();

            var nextIds = new HashSet<int>();

            foreach (var rel in rels)
            {
                if (!visitedIds.Contains(rel.FromEntityId))
                {
                    nextIds.Add(rel.FromEntityId);
                    visitedIds.Add(rel.FromEntityId);
                }
                if (!visitedIds.Contains(rel.ToEntityId))
                {
                    nextIds.Add(rel.ToEntityId);
                    visitedIds.Add(rel.ToEntityId);
                }

                graph.Edges.Add(new NetworkEdge
                {
                    From = rel.FromEntityId.ToString(),
                    To = rel.ToEntityId.ToString(),
                    Label = rel.Type,
                    RelationshipType = rel.Type
                });
            }

            currentIds = nextIds;
        }

        // Load all visited entities
        var entities = await _context.Entities
            .Where(e => visitedIds.Contains(e.Id))
            .ToListAsync();

        foreach (var entity in entities)
        {
            graph.Nodes.Add(new NetworkNode
            {
                Id = entity.Id.ToString(),
                Label = entity.Name,
                Group = entity.Type,
                Title = $"{entity.Name} ({entity.Type})"
            });
        }

        return Ok(graph);
    }

    /// <summary>
    /// Find the shortest path between two entities using BFS
    /// </summary>
    [HttpGet("path")]
    public async Task<ActionResult<PathResult>> FindPath([FromQuery] string from, [FromQuery] string to)
    {
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
            return BadRequest("Both 'from' and 'to' entity IDs are required");

        var fromEntity = await FindEntityByIdAsync(from);
        var toEntity = await FindEntityByIdAsync(to);

        if (fromEntity == null || toEntity == null)
            return Ok(new PathResult());

        // BFS to find shortest path
        var allRels = await _context.Relationships.ToListAsync();
        var adjacency = new Dictionary<int, List<(int neighbor, Relationship rel)>>();

        foreach (var rel in allRels)
        {
            if (!adjacency.ContainsKey(rel.FromEntityId))
                adjacency[rel.FromEntityId] = new();
            if (!adjacency.ContainsKey(rel.ToEntityId))
                adjacency[rel.ToEntityId] = new();

            adjacency[rel.FromEntityId].Add((rel.ToEntityId, rel));
            adjacency[rel.ToEntityId].Add((rel.FromEntityId, rel));
        }

        // BFS
        var visited = new HashSet<int> { fromEntity.Id };
        var queue = new Queue<(int nodeId, List<int> path, List<Relationship> edges)>();
        queue.Enqueue((fromEntity.Id, new List<int> { fromEntity.Id }, new List<Relationship>()));

        while (queue.Count > 0)
        {
            var (current, path, edges) = queue.Dequeue();

            if (current == toEntity.Id)
            {
                // Found the path
                var pathResult = new PathResult { Length = edges.Count };
                var pathEntities = await _context.Entities
                    .Where(e => path.Contains(e.Id))
                    .ToListAsync();

                foreach (var entity in pathEntities)
                {
                    pathResult.Nodes.Add(new NetworkNode
                    {
                        Id = entity.Id.ToString(),
                        Label = entity.Name,
                        Group = entity.Type
                    });
                }

                foreach (var rel in edges)
                {
                    pathResult.Edges.Add(new NetworkEdge
                    {
                        From = rel.FromEntityId.ToString(),
                        To = rel.ToEntityId.ToString(),
                        Label = rel.Type,
                        RelationshipType = rel.Type
                    });
                }

                return Ok(pathResult);
            }

            if (adjacency.ContainsKey(current))
            {
                foreach (var (neighbor, rel) in adjacency[current])
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        var newPath = new List<int>(path) { neighbor };
                        var newEdges = new List<Relationship>(edges) { rel };
                        queue.Enqueue((neighbor, newPath, newEdges));
                    }
                }
            }
        }

        return Ok(new PathResult());
    }

    /// <summary>
    /// Get network metrics (centrality indicators, density, etc.)
    /// </summary>
    [HttpGet("metrics")]
    public async Task<ActionResult<NetworkMetrics>> GetMetrics()
    {
        var metrics = new NetworkMetrics();

        metrics.TotalNodes = await _context.Entities.CountAsync();
        metrics.TotalEdges = await _context.Relationships.CountAsync();

        if (metrics.TotalNodes > 1)
        {
            metrics.Density = (2.0 * metrics.TotalEdges) / (metrics.TotalNodes * (metrics.TotalNodes - 1));
        }

        // Degree centrality: count connections per entity
        var fromCounts = await _context.Relationships
            .GroupBy(r => r.FromEntityId)
            .Select(g => new { EntityId = g.Key, Count = g.Count() })
            .ToListAsync();

        var toCounts = await _context.Relationships
            .GroupBy(r => r.ToEntityId)
            .Select(g => new { EntityId = g.Key, Count = g.Count() })
            .ToListAsync();

        var degreeCounts = new Dictionary<int, int>();
        foreach (var fc in fromCounts)
        {
            degreeCounts[fc.EntityId] = fc.Count;
        }
        foreach (var tc in toCounts)
        {
            if (degreeCounts.ContainsKey(tc.EntityId))
                degreeCounts[tc.EntityId] += tc.Count;
            else
                degreeCounts[tc.EntityId] = tc.Count;
        }

        // Get entity names for top degree nodes
        var topIds = degreeCounts.OrderByDescending(kv => kv.Value).Take(20).Select(kv => kv.Key).ToList();
        var topEntities = await _context.Entities
            .Where(e => topIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.Name);

        foreach (var kv in degreeCounts.OrderByDescending(kv => kv.Value).Take(20))
        {
            var name = topEntities.ContainsKey(kv.Key) ? topEntities[kv.Key] : kv.Key.ToString();
            metrics.DegreeCentrality[name] = (double)kv.Value / (metrics.TotalNodes - 1);
        }

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
        var filter = new NetworkFilter { StartDate = startDate, EndDate = endDate };
        return await GetNetwork(filter);
    }

    /// <summary>
    /// Seed the MySQL relationships data
    /// </summary>
    [HttpPost("seed")]
    public async Task<ActionResult> SeedData()
    {
        if (!await _context.Relationships.AnyAsync())
        {
            // Re-run the full seed which now includes relationships
            await Data.SeedData.InitializeAsync(_context);
        }
        return Ok(new { message = "Network data seeded successfully" });
    }

    private async Task<Entity?> FindEntityByIdAsync(string entityId)
    {
        // Try to parse as direct numeric ID
        if (int.TryParse(entityId, out var numericId))
        {
            return await _context.Entities.FindAsync(numericId);
        }

        // Try pattern like "C1", "P2" etc.
        var type = entityId[0] switch
        {
            'C' or 'c' => "Claimant",
            'P' or 'p' => "Provider",
            _ => null
        };

        if (type != null && int.TryParse(entityId.Substring(1), out var index))
        {
            return await _context.Entities
                .Where(e => e.Type == type)
                .OrderBy(e => e.Id)
                .Skip(index - 1)
                .FirstOrDefaultAsync();
        }

        // Try matching by name
        return await _context.Entities
            .FirstOrDefaultAsync(e => e.Name.Contains(entityId));
    }
}
