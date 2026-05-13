using CrimsonDnsManager.App.Models;
using Microsoft.EntityFrameworkCore;

namespace CrimsonDnsManager.App.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppSettings> Settings => Set<AppSettings>();
    public DbSet<StoredZone> StoredZones => Set<StoredZone>();
    public DbSet<StoredLivefeedEntry> StoredLivefeedEntries => Set<StoredLivefeedEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppSettings>().HasKey(x => x.Id);
        modelBuilder.Entity<AppSettings>().Property(x => x.Id).ValueGeneratedNever();

        modelBuilder.Entity<StoredZone>().HasKey(x => x.Id);
        modelBuilder.Entity<StoredZone>().Property(x => x.Name).IsRequired();

        modelBuilder.Entity<StoredLivefeedEntry>().HasKey(x => x.Id);
        modelBuilder.Entity<StoredLivefeedEntry>().Property(x => x.Message).IsRequired();
    }
}

public sealed class StoredZone
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsLastActive { get; set; }
}

public sealed class StoredLivefeedEntry
{
    public int Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public LivefeedLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
}
