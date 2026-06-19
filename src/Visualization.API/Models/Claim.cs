namespace Visualization.API.Models;

public class Claim
{
    public int Id { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Open, Closed, Investigating
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty; // Auto, Health, Property
    public DateTime IncidentDate { get; set; }
    public DateTime FiledDate { get; set; }
    public double? IncidentLatitude { get; set; }
    public double? IncidentLongitude { get; set; }
    public string? IncidentLocation { get; set; }
    public string? Description { get; set; }

    public int EntityId { get; set; }
    public Entity Entity { get; set; } = null!;
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}
