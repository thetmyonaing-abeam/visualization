namespace Visualization.API.Models;

public class Alert
{
    public int Id { get; set; }
    public string AlertType { get; set; } = string.Empty; // Fraud, Duplicate, Anomaly
    public string Severity { get; set; } = string.Empty; // Low, Medium, High, Critical
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public int? EntityId { get; set; }
    public Entity? Entity { get; set; }
    public int? ClaimId { get; set; }
    public Claim? Claim { get; set; }
}
