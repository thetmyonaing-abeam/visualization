using Visualization.API.Models;

namespace Visualization.API.Data;

public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        if (context.Entities.Any()) return;

        // Providers
        var providers = new List<Entity>
        {
            new() { Name = "City Medical Center", Type = "Provider", Latitude = 40.7128, Longitude = -74.0060, Address = "123 Main St, New York, NY", Phone = "212-555-0100", Email = "admin@citymedical.com" },
            new() { Name = "Metro Auto Repair", Type = "Provider", Latitude = 40.7580, Longitude = -73.9855, Address = "456 Broadway, New York, NY", Phone = "212-555-0200", Email = "info@metroauto.com" },
            new() { Name = "Downtown Health Clinic", Type = "Provider", Latitude = 40.7282, Longitude = -73.7949, Address = "789 Oak Ave, Queens, NY", Phone = "718-555-0300", Email = "contact@dthc.com" },
            new() { Name = "Express Body Shop", Type = "Provider", Latitude = 40.6892, Longitude = -74.0445, Address = "321 Harbor Rd, Brooklyn, NY", Phone = "718-555-0400", Email = "shop@expressbody.com" },
            new() { Name = "Premier Physical Therapy", Type = "Provider", Latitude = 40.7484, Longitude = -73.9857, Address = "555 Park Ave, New York, NY", Phone = "212-555-0500", Email = "pt@premierpt.com" },
        };

        // Claimants
        var claimants = new List<Entity>
        {
            new() { Name = "John Smith", Type = "Claimant", Latitude = 40.7549, Longitude = -73.9840, Address = "101 E 52nd St, New York, NY", Phone = "212-555-1001", Email = "john.smith@email.com" },
            new() { Name = "Jane Doe", Type = "Claimant", Latitude = 40.7282, Longitude = -73.7949, Address = "202 Queens Blvd, Queens, NY", Phone = "718-555-1002", Email = "jane.doe@email.com" },
            new() { Name = "Robert Johnson", Type = "Claimant", Latitude = 40.6782, Longitude = -73.9442, Address = "303 Atlantic Ave, Brooklyn, NY", Phone = "718-555-1003", Email = "r.johnson@email.com" },
            new() { Name = "Maria Garcia", Type = "Claimant", Latitude = 40.7589, Longitude = -73.9851, Address = "404 W 47th St, New York, NY", Phone = "212-555-1004", Email = "m.garcia@email.com" },
            new() { Name = "David Brown", Type = "Claimant", Latitude = 40.7614, Longitude = -73.9776, Address = "505 5th Ave, New York, NY", Phone = "212-555-1005", Email = "d.brown@email.com" },
            new() { Name = "Lisa Wilson", Type = "Claimant", Latitude = 40.7308, Longitude = -73.9973, Address = "606 Bleecker St, New York, NY", Phone = "212-555-1006", Email = "l.wilson@email.com" },
            new() { Name = "Michael Chen", Type = "Claimant", Latitude = 40.7580, Longitude = -73.8855, Address = "707 Main St, Flushing, NY", Phone = "718-555-1007", Email = "m.chen@email.com" },
            new() { Name = "Sarah Taylor", Type = "Claimant", Latitude = 40.6501, Longitude = -73.9496, Address = "808 Flatbush Ave, Brooklyn, NY", Phone = "718-555-1008", Email = "s.taylor@email.com" },
        };

        // Policies
        var policies = new List<Entity>
        {
            new() { Name = "POL-2024-001", Type = "Policy", Metadata = "{\"coverage\":\"Full\",\"premium\":1200}" },
            new() { Name = "POL-2024-002", Type = "Policy", Metadata = "{\"coverage\":\"Liability\",\"premium\":800}" },
            new() { Name = "POL-2024-003", Type = "Policy", Metadata = "{\"coverage\":\"Full\",\"premium\":1500}" },
            new() { Name = "POL-2024-004", Type = "Policy", Metadata = "{\"coverage\":\"Health\",\"premium\":500}" },
            new() { Name = "POL-2024-005", Type = "Policy", Metadata = "{\"coverage\":\"Property\",\"premium\":900}" },
        };

        context.Entities.AddRange(providers);
        context.Entities.AddRange(claimants);
        context.Entities.AddRange(policies);
        await context.SaveChangesAsync();

        // Claims
        var claims = new List<Claim>
        {
            new() { ClaimNumber = "CLM-2024-001", Status = "Investigating", Amount = 15000, Type = "Auto", IncidentDate = new DateTime(2024, 3, 15), FiledDate = new DateTime(2024, 3, 18), IncidentLatitude = 40.7128, IncidentLongitude = -74.0060, IncidentLocation = "Intersection of Broadway & 42nd St", Description = "Multi-vehicle collision", EntityId = claimants[0].Id },
            new() { ClaimNumber = "CLM-2024-002", Status = "Open", Amount = 8500, Type = "Health", IncidentDate = new DateTime(2024, 4, 1), FiledDate = new DateTime(2024, 4, 3), IncidentLatitude = 40.7484, IncidentLongitude = -73.9857, IncidentLocation = "City Medical Center", Description = "Slip and fall injury", EntityId = claimants[1].Id },
            new() { ClaimNumber = "CLM-2024-003", Status = "Closed", Amount = 22000, Type = "Auto", IncidentDate = new DateTime(2024, 2, 28), FiledDate = new DateTime(2024, 3, 2), IncidentLatitude = 40.6892, IncidentLongitude = -74.0445, IncidentLocation = "Brooklyn Bridge approach", Description = "Rear-end collision, total loss", EntityId = claimants[2].Id },
            new() { ClaimNumber = "CLM-2024-004", Status = "Investigating", Amount = 45000, Type = "Health", IncidentDate = new DateTime(2024, 5, 10), FiledDate = new DateTime(2024, 5, 12), IncidentLatitude = 40.7282, IncidentLongitude = -73.7949, IncidentLocation = "Downtown Health Clinic", Description = "Extensive treatment for back injury", EntityId = claimants[3].Id },
            new() { ClaimNumber = "CLM-2024-005", Status = "Open", Amount = 12000, Type = "Auto", IncidentDate = new DateTime(2024, 5, 20), FiledDate = new DateTime(2024, 5, 22), IncidentLatitude = 40.7589, IncidentLongitude = -73.9851, IncidentLocation = "Times Square area", Description = "Hit and run incident", EntityId = claimants[4].Id },
            new() { ClaimNumber = "CLM-2024-006", Status = "Investigating", Amount = 35000, Type = "Property", IncidentDate = new DateTime(2024, 6, 1), FiledDate = new DateTime(2024, 6, 3), IncidentLatitude = 40.7308, IncidentLongitude = -73.9973, IncidentLocation = "606 Bleecker St", Description = "Water damage from burst pipe", EntityId = claimants[5].Id },
            new() { ClaimNumber = "CLM-2024-007", Status = "Open", Amount = 9500, Type = "Auto", IncidentDate = new DateTime(2024, 6, 15), FiledDate = new DateTime(2024, 6, 17), IncidentLatitude = 40.7580, IncidentLongitude = -73.8855, IncidentLocation = "Northern Blvd, Flushing", Description = "Side collision at intersection", EntityId = claimants[6].Id },
            new() { ClaimNumber = "CLM-2024-008", Status = "Investigating", Amount = 18000, Type = "Health", IncidentDate = new DateTime(2024, 6, 20), FiledDate = new DateTime(2024, 6, 22), IncidentLatitude = 40.6501, IncidentLongitude = -73.9496, IncidentLocation = "Flatbush area", Description = "Multiple treatment sessions, possible over-billing", EntityId = claimants[7].Id },
        };

        context.Claims.AddRange(claims);
        await context.SaveChangesAsync();

        // Alerts
        var alerts = new List<Alert>
        {
            new() { AlertType = "Fraud", Severity = "Critical", Description = "Multiple claims from same provider within 30 days with similar patterns", CreatedAt = new DateTime(2024, 5, 15), EntityId = providers[0].Id, ClaimId = claims[1].Id },
            new() { AlertType = "Duplicate", Severity = "High", Description = "Duplicate billing detected for same procedure on same date", CreatedAt = new DateTime(2024, 4, 20), EntityId = providers[2].Id, ClaimId = claims[3].Id },
            new() { AlertType = "Anomaly", Severity = "Medium", Description = "Claim amount significantly exceeds average for this type", CreatedAt = new DateTime(2024, 5, 25), EntityId = claimants[3].Id, ClaimId = claims[3].Id },
            new() { AlertType = "Fraud", Severity = "High", Description = "Claimant linked to multiple suspicious claims across providers", CreatedAt = new DateTime(2024, 6, 5), EntityId = claimants[0].Id, ClaimId = claims[0].Id },
            new() { AlertType = "Anomaly", Severity = "Low", Description = "Treatment duration exceeds typical recovery time", CreatedAt = new DateTime(2024, 6, 10), EntityId = providers[4].Id, ClaimId = claims[7].Id },
            new() { AlertType = "Fraud", Severity = "Critical", Description = "Network of entities with circular referral pattern detected", CreatedAt = new DateTime(2024, 6, 18), EntityId = providers[0].Id },
            new() { AlertType = "Duplicate", Severity = "Medium", Description = "Similar claim filed by different entity at same location", CreatedAt = new DateTime(2024, 6, 20), ClaimId = claims[4].Id },
        };

        context.Alerts.AddRange(alerts);
        await context.SaveChangesAsync();

        // Relationships (for network visualization)
        var relationships = new List<Relationship>
        {
            new() { FromEntityId = claimants[0].Id, ToEntityId = providers[0].Id, Type = "FILED_CLAIM_AT", CreatedAt = new DateTime(2024, 3, 15) },
            new() { FromEntityId = claimants[0].Id, ToEntityId = providers[1].Id, Type = "FILED_CLAIM_AT", CreatedAt = new DateTime(2024, 3, 20) },
            new() { FromEntityId = claimants[1].Id, ToEntityId = providers[0].Id, Type = "FILED_CLAIM_AT", CreatedAt = new DateTime(2024, 4, 1) },
            new() { FromEntityId = claimants[1].Id, ToEntityId = providers[2].Id, Type = "FILED_CLAIM_AT", CreatedAt = new DateTime(2024, 4, 5) },
            new() { FromEntityId = claimants[2].Id, ToEntityId = providers[3].Id, Type = "FILED_CLAIM_AT", CreatedAt = new DateTime(2024, 2, 28) },
            new() { FromEntityId = claimants[3].Id, ToEntityId = providers[2].Id, Type = "FILED_CLAIM_AT", CreatedAt = new DateTime(2024, 5, 10) },
            new() { FromEntityId = claimants[3].Id, ToEntityId = providers[4].Id, Type = "FILED_CLAIM_AT", CreatedAt = new DateTime(2024, 5, 15) },
            new() { FromEntityId = claimants[4].Id, ToEntityId = providers[1].Id, Type = "FILED_CLAIM_AT", CreatedAt = new DateTime(2024, 5, 20) },
            new() { FromEntityId = claimants[5].Id, ToEntityId = providers[0].Id, Type = "FILED_CLAIM_AT", CreatedAt = new DateTime(2024, 6, 1) },
            new() { FromEntityId = claimants[6].Id, ToEntityId = providers[1].Id, Type = "FILED_CLAIM_AT", CreatedAt = new DateTime(2024, 6, 15) },
            new() { FromEntityId = claimants[7].Id, ToEntityId = providers[4].Id, Type = "FILED_CLAIM_AT", CreatedAt = new DateTime(2024, 6, 20) },
            new() { FromEntityId = claimants[7].Id, ToEntityId = providers[2].Id, Type = "FILED_CLAIM_AT", CreatedAt = new DateTime(2024, 6, 22) },
            new() { FromEntityId = providers[0].Id, ToEntityId = providers[4].Id, Type = "REFERRED_TO", CreatedAt = new DateTime(2024, 4, 10) },
            new() { FromEntityId = providers[2].Id, ToEntityId = providers[4].Id, Type = "REFERRED_TO", CreatedAt = new DateTime(2024, 5, 20) },
            new() { FromEntityId = providers[4].Id, ToEntityId = providers[0].Id, Type = "REFERRED_TO", CreatedAt = new DateTime(2024, 6, 1) },
            new() { FromEntityId = claimants[0].Id, ToEntityId = claimants[3].Id, Type = "KNOWS", CreatedAt = new DateTime(2024, 5, 1) },
            new() { FromEntityId = claimants[1].Id, ToEntityId = claimants[2].Id, Type = "KNOWS", CreatedAt = new DateTime(2024, 3, 1) },
            new() { FromEntityId = claimants[3].Id, ToEntityId = claimants[5].Id, Type = "SAME_ADDRESS", CreatedAt = new DateTime(2024, 1, 1) },
        };

        context.Relationships.AddRange(relationships);
        await context.SaveChangesAsync();
    }
}
