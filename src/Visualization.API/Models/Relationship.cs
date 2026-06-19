namespace Visualization.API.Models;

public class Relationship
{
    public int Id { get; set; }
    public int FromEntityId { get; set; }
    public int ToEntityId { get; set; }
    public string Type { get; set; } = string.Empty; // FILED_CLAIM_AT, REFERRED_TO, KNOWS, SAME_ADDRESS
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Metadata { get; set; }

    public Entity FromEntity { get; set; } = null!;
    public Entity ToEntity { get; set; } = null!;
}
