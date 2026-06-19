using Microsoft.EntityFrameworkCore;
using Visualization.API.Models;

namespace Visualization.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Entity> Entities { get; set; }
    public DbSet<Claim> Claims { get; set; }
    public DbSet<Alert> Alerts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Entity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Type).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.HasIndex(e => e.Type);
        });

        modelBuilder.Entity<Claim>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.ClaimNumber).HasMaxLength(50).IsRequired();
            entity.Property(c => c.Status).HasMaxLength(50).IsRequired();
            entity.Property(c => c.Type).HasMaxLength(50).IsRequired();
            entity.Property(c => c.Amount).HasColumnType("decimal(18,2)");
            entity.Property(c => c.IncidentLocation).HasMaxLength(500);
            entity.HasIndex(c => c.ClaimNumber).IsUnique();
            entity.HasIndex(c => c.Status);
            entity.HasOne(c => c.Entity)
                  .WithMany(e => e.Claims)
                  .HasForeignKey(c => c.EntityId);
        });

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.AlertType).HasMaxLength(50).IsRequired();
            entity.Property(a => a.Severity).HasMaxLength(20).IsRequired();
            entity.Property(a => a.Description).HasMaxLength(1000).IsRequired();
            entity.HasIndex(a => a.Severity);
            entity.HasIndex(a => a.AlertType);
            entity.HasOne(a => a.Entity)
                  .WithMany(e => e.Alerts)
                  .HasForeignKey(a => a.EntityId);
            entity.HasOne(a => a.Claim)
                  .WithMany(c => c.Alerts)
                  .HasForeignKey(a => a.ClaimId);
        });
    }
}
