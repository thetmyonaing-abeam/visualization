namespace Visualization.API.Models;

public class NetworkNode
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string? Title { get; set; }
    public int? Size { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
}

public class NetworkEdge
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string? Label { get; set; }
    public string? RelationshipType { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
}

public class NetworkGraph
{
    public List<NetworkNode> Nodes { get; set; } = new();
    public List<NetworkEdge> Edges { get; set; } = new();
}

public class NetworkMetrics
{
    public Dictionary<string, double> DegreeCentrality { get; set; } = new();
    public Dictionary<string, double> BetweennessCentrality { get; set; } = new();
    public Dictionary<string, double> ClosenessCentrality { get; set; } = new();
    public int TotalNodes { get; set; }
    public int TotalEdges { get; set; }
    public double Density { get; set; }
}

public class PathResult
{
    public List<NetworkNode> Nodes { get; set; } = new();
    public List<NetworkEdge> Edges { get; set; } = new();
    public int Length { get; set; }
    public double? TotalWeight { get; set; }
}

public class NetworkFilter
{
    public List<string>? EntityTypes { get; set; }
    public List<string>? RelationshipTypes { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SeverityFilter { get; set; }
    public int? MaxDepth { get; set; }
}
