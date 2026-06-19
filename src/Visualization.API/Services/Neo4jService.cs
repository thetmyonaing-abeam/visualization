using Neo4j.Driver;
using Visualization.API.Models;

namespace Visualization.API.Services;

public interface INeo4jService
{
    Task<NetworkGraph> GetFullNetworkAsync(NetworkFilter? filter = null);
    Task<NetworkGraph> GetEntityNetworkAsync(string entityId, int depth = 2);
    Task<PathResult> FindShortestPathAsync(string fromId, string toId);
    Task<NetworkMetrics> GetNetworkMetricsAsync();
    Task<NetworkGraph> GetTimeBasedNetworkAsync(DateTime startDate, DateTime endDate);
    Task SeedGraphDataAsync();
}

public class Neo4jService : INeo4jService, IAsyncDisposable
{
    private readonly IDriver _driver;
    private readonly ILogger<Neo4jService> _logger;

    public Neo4jService(IConfiguration configuration, ILogger<Neo4jService> logger)
    {
        _logger = logger;
        var uri = configuration["Neo4j:Uri"] ?? "bolt://localhost:7687";
        var user = configuration["Neo4j:User"] ?? "neo4j";
        var password = configuration["Neo4j:Password"] ?? "password123";
        _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
    }

    public async Task<NetworkGraph> GetFullNetworkAsync(NetworkFilter? filter = null)
    {
        var graph = new NetworkGraph();
        await using var session = _driver.AsyncSession();

        var cypher = "MATCH (n) OPTIONAL MATCH (n)-[r]->(m) RETURN n, r, m";

        if (filter?.EntityTypes?.Any() == true)
        {
            var types = string.Join("','", filter.EntityTypes);
            cypher = $"MATCH (n) WHERE n.type IN ['{types}'] OPTIONAL MATCH (n)-[r]->(m) RETURN n, r, m";
        }

        if (filter?.StartDate != null && filter?.EndDate != null)
        {
            cypher = $"MATCH (n) WHERE n.createdAt >= '{filter.StartDate:yyyy-MM-dd}' AND n.createdAt <= '{filter.EndDate:yyyy-MM-dd}' OPTIONAL MATCH (n)-[r]->(m) RETURN n, r, m";
        }

        try
        {
            var result = await session.RunAsync(cypher);
            var records = await result.ToListAsync();

            var nodeIds = new HashSet<string>();

            foreach (var record in records)
            {
                var node = record["n"].As<INode>();
                var nodeId = node.ElementId;

                if (nodeIds.Add(nodeId))
                {
                    graph.Nodes.Add(new NetworkNode
                    {
                        Id = nodeId,
                        Label = node.Properties.ContainsKey("name") ? node.Properties["name"].As<string>() : nodeId,
                        Group = node.Properties.ContainsKey("type") ? node.Properties["type"].As<string>() : node.Labels.FirstOrDefault() ?? "Unknown",
                        Properties = node.Properties.ToDictionary(p => p.Key, p => p.Value)
                    });
                }

                if (record["m"] != null && record["m"] is not null && record["m"] is INode)
                {
                    var targetNode = record["m"].As<INode>();
                    var targetId = targetNode.ElementId;

                    if (nodeIds.Add(targetId))
                    {
                        graph.Nodes.Add(new NetworkNode
                        {
                            Id = targetId,
                            Label = targetNode.Properties.ContainsKey("name") ? targetNode.Properties["name"].As<string>() : targetId,
                            Group = targetNode.Properties.ContainsKey("type") ? targetNode.Properties["type"].As<string>() : targetNode.Labels.FirstOrDefault() ?? "Unknown",
                            Properties = targetNode.Properties.ToDictionary(p => p.Key, p => p.Value)
                        });
                    }

                    if (record["r"] != null && record["r"] is IRelationship)
                    {
                        var rel = record["r"].As<IRelationship>();
                        graph.Edges.Add(new NetworkEdge
                        {
                            From = nodeId,
                            To = targetId,
                            Label = rel.Type,
                            RelationshipType = rel.Type,
                            Properties = rel.Properties.ToDictionary(p => p.Key, p => p.Value)
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching network from Neo4j");
        }

        return graph;
    }

    public async Task<NetworkGraph> GetEntityNetworkAsync(string entityId, int depth = 2)
    {
        var graph = new NetworkGraph();
        await using var session = _driver.AsyncSession();

        var cypher = $"MATCH path = (n {{entityId: '{entityId}'}})-[*1..{depth}]-(m) " +
                     "UNWIND nodes(path) AS node " +
                     "UNWIND relationships(path) AS rel " +
                     "RETURN DISTINCT node, rel";

        try
        {
            var result = await session.RunAsync(cypher);
            var records = await result.ToListAsync();
            var nodeIds = new HashSet<string>();

            foreach (var record in records)
            {
                var node = record["node"].As<INode>();
                var nodeId = node.ElementId;

                if (nodeIds.Add(nodeId))
                {
                    graph.Nodes.Add(new NetworkNode
                    {
                        Id = nodeId,
                        Label = node.Properties.ContainsKey("name") ? node.Properties["name"].As<string>() : nodeId,
                        Group = node.Properties.ContainsKey("type") ? node.Properties["type"].As<string>() : node.Labels.FirstOrDefault() ?? "Unknown",
                        Properties = node.Properties.ToDictionary(p => p.Key, p => p.Value)
                    });
                }

                if (record["rel"] != null && record["rel"] is IRelationship)
                {
                    var rel = record["rel"].As<IRelationship>();
                    graph.Edges.Add(new NetworkEdge
                    {
                        From = rel.StartNodeElementId,
                        To = rel.EndNodeElementId,
                        Label = rel.Type,
                        RelationshipType = rel.Type,
                        Properties = rel.Properties.ToDictionary(p => p.Key, p => p.Value)
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching entity network for {EntityId}", entityId);
        }

        return graph;
    }

    public async Task<PathResult> FindShortestPathAsync(string fromId, string toId)
    {
        var pathResult = new PathResult();
        await using var session = _driver.AsyncSession();

        var cypher = $"MATCH path = shortestPath((a {{entityId: '{fromId}'}})-[*]-(b {{entityId: '{toId}'}})) " +
                     "RETURN path";

        try
        {
            var result = await session.RunAsync(cypher);
            var records = await result.ToListAsync();

            if (records.Any())
            {
                var path = records.First()["path"].As<IPath>();
                pathResult.Length = path.Relationships.Count();

                foreach (var node in path.Nodes)
                {
                    pathResult.Nodes.Add(new NetworkNode
                    {
                        Id = node.ElementId,
                        Label = node.Properties.ContainsKey("name") ? node.Properties["name"].As<string>() : node.ElementId,
                        Group = node.Properties.ContainsKey("type") ? node.Properties["type"].As<string>() : node.Labels.FirstOrDefault() ?? "Unknown"
                    });
                }

                foreach (var rel in path.Relationships)
                {
                    pathResult.Edges.Add(new NetworkEdge
                    {
                        From = rel.StartNodeElementId,
                        To = rel.EndNodeElementId,
                        Label = rel.Type,
                        RelationshipType = rel.Type
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding shortest path from {From} to {To}", fromId, toId);
        }

        return pathResult;
    }

    public async Task<NetworkMetrics> GetNetworkMetricsAsync()
    {
        var metrics = new NetworkMetrics();
        await using var session = _driver.AsyncSession();

        try
        {
            // Total nodes and edges
            var countResult = await session.RunAsync("MATCH (n) RETURN count(n) as nodeCount");
            var countRecords = await countResult.ToListAsync();
            metrics.TotalNodes = countRecords.First()["nodeCount"].As<int>();

            var edgeResult = await session.RunAsync("MATCH ()-[r]->() RETURN count(r) as edgeCount");
            var edgeRecords = await edgeResult.ToListAsync();
            metrics.TotalEdges = edgeRecords.First()["edgeCount"].As<int>();

            // Density
            if (metrics.TotalNodes > 1)
            {
                metrics.Density = (2.0 * metrics.TotalEdges) / (metrics.TotalNodes * (metrics.TotalNodes - 1));
            }

            // Degree centrality
            var degreeResult = await session.RunAsync(
                "MATCH (n) " +
                "OPTIONAL MATCH (n)-[r]-() " +
                "RETURN n.entityId as id, n.name as name, count(r) as degree " +
                "ORDER BY degree DESC LIMIT 20");
            var degreeRecords = await degreeResult.ToListAsync();

            foreach (var record in degreeRecords)
            {
                var id = record["id"]?.As<string>() ?? record["name"]?.As<string>() ?? "unknown";
                var degree = record["degree"].As<int>();
                if (metrics.TotalNodes > 1)
                {
                    metrics.DegreeCentrality[id] = (double)degree / (metrics.TotalNodes - 1);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating network metrics");
        }

        return metrics;
    }

    public async Task<NetworkGraph> GetTimeBasedNetworkAsync(DateTime startDate, DateTime endDate)
    {
        var filter = new NetworkFilter { StartDate = startDate, EndDate = endDate };
        return await GetFullNetworkAsync(filter);
    }

    public async Task SeedGraphDataAsync()
    {
        await using var session = _driver.AsyncSession();

        try
        {
            // Clear existing data
            await session.RunAsync("MATCH (n) DETACH DELETE n");

            // Create entity nodes
            var entities = new[]
            {
                ("P1", "City Medical Center", "Provider", "2024-01-01"),
                ("P2", "Metro Auto Repair", "Provider", "2024-01-01"),
                ("P3", "Downtown Health Clinic", "Provider", "2024-01-15"),
                ("P4", "Express Body Shop", "Provider", "2024-02-01"),
                ("P5", "Premier Physical Therapy", "Provider", "2024-02-15"),
                ("C1", "John Smith", "Claimant", "2024-03-15"),
                ("C2", "Jane Doe", "Claimant", "2024-04-01"),
                ("C3", "Robert Johnson", "Claimant", "2024-02-28"),
                ("C4", "Maria Garcia", "Claimant", "2024-05-10"),
                ("C5", "David Brown", "Claimant", "2024-05-20"),
                ("C6", "Lisa Wilson", "Claimant", "2024-06-01"),
                ("C7", "Michael Chen", "Claimant", "2024-06-15"),
                ("C8", "Sarah Taylor", "Claimant", "2024-06-20"),
            };

            foreach (var (id, name, type, date) in entities)
            {
                await session.RunAsync(
                    $"CREATE (:{type} {{entityId: '{id}', name: '{name}', type: '{type}', createdAt: '{date}'}})");
            }

            // Create relationships
            var relationships = new[]
            {
                ("C1", "FILED_CLAIM_AT", "P1", "2024-03-15"),
                ("C1", "FILED_CLAIM_AT", "P2", "2024-03-20"),
                ("C2", "FILED_CLAIM_AT", "P1", "2024-04-01"),
                ("C2", "FILED_CLAIM_AT", "P3", "2024-04-05"),
                ("C3", "FILED_CLAIM_AT", "P4", "2024-02-28"),
                ("C4", "FILED_CLAIM_AT", "P3", "2024-05-10"),
                ("C4", "FILED_CLAIM_AT", "P5", "2024-05-15"),
                ("C5", "FILED_CLAIM_AT", "P2", "2024-05-20"),
                ("C6", "FILED_CLAIM_AT", "P1", "2024-06-01"),
                ("C7", "FILED_CLAIM_AT", "P2", "2024-06-15"),
                ("C8", "FILED_CLAIM_AT", "P5", "2024-06-20"),
                ("C8", "FILED_CLAIM_AT", "P3", "2024-06-22"),
                ("P1", "REFERRED_TO", "P5", "2024-04-10"),
                ("P3", "REFERRED_TO", "P5", "2024-05-20"),
                ("P5", "REFERRED_TO", "P1", "2024-06-01"),
                ("C1", "KNOWS", "C4", "2024-05-01"),
                ("C2", "KNOWS", "C3", "2024-03-01"),
                ("C4", "SAME_ADDRESS", "C6", "2024-01-01"),
            };

            foreach (var (from, relType, to, date) in relationships)
            {
                await session.RunAsync(
                    $"MATCH (a {{entityId: '{from}'}}), (b {{entityId: '{to}'}}) " +
                    $"CREATE (a)-[:{relType} {{date: '{date}'}}]->(b)");
            }

            _logger.LogInformation("Neo4j graph data seeded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding Neo4j data");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _driver.DisposeAsync();
    }
}
