using API.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Dataset> Datasets => Set<Dataset>();
    public DbSet<SimCard> SimCards => Set<SimCard>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Dataset>()
            .Property(d => d.UploadDate)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc).ToLocalTime()
            );

        modelBuilder.Entity<SimCard>()
            .Property(d => d.AddedDate)
            .HasConversion(
                v => v.HasValue ? v.Value.ToUniversalTime() : v.Value,
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc).ToLocalTime()
            );

        modelBuilder.Entity<Dataset>()
            .HasIndex(d => d.FileHash)
            .IsUnique();
    }
}
