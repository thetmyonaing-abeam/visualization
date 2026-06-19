namespace Visualization.API.Models;

public class Entity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Provider, Claimant, Policy, Vehicle
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Metadata { get; set; } // JSON metadata

    public ICollection<Claim> Claims { get; set; } = new List<Claim>();
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}
