using CrimsonDnsManager.App.Data;
using CrimsonDnsManager.App.Models;
using Microsoft.EntityFrameworkCore;

namespace CrimsonDnsManager.App.Services;

public sealed class SettingsService(AppDbContext dbContext, ISecretService secretService, LivefeedService livefeed)
{
    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = await dbContext.Settings.FirstOrDefaultAsync(x => x.Id == 1, cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = new AppSettings
        {
            Id = 1,
            Domain = "cr1ms0n.xyz",
            ZoneId = string.Empty,
            AccountId = string.Empty,
            DefaultWebRecordsProxied = true,
            BackupBeforeSave = true
        };

        dbContext.Settings.Add(settings);
        await dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }

    public async Task SaveAsync(AppSettings settings, string? token, CancellationToken cancellationToken = default)
    {
        settings.Id = 1;
        var existing = await dbContext.Settings.FirstOrDefaultAsync(x => x.Id == 1, cancellationToken);

        if (existing is null)
        {
            dbContext.Settings.Add(settings);
        }
        else
        {
            existing.Domain = settings.Domain.Trim();
            existing.ZoneId = settings.ZoneId.Trim();
            existing.AccountId = settings.AccountId?.Trim();
            existing.DefaultWebRecordsProxied = settings.DefaultWebRecordsProxied;
            existing.BackupBeforeSave = settings.BackupBeforeSave;
            existing.LastActiveZoneId = settings.LastActiveZoneId;
            existing.LastActiveZoneName = settings.LastActiveZoneName;
        }

        if (!string.IsNullOrWhiteSpace(token) && token != CloudflareSettingsViewModelMask.MaskedToken)
        {
            await secretService.SaveCloudflareTokenAsync(token, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        livefeed.Success("Einstellungen gespeichert.");
    }

    public async Task StoreZonesAsync(IEnumerable<CloudflareZone> zones, string? activeZoneId, CancellationToken cancellationToken = default)
    {
        dbContext.StoredZones.RemoveRange(dbContext.StoredZones);
        foreach (var zone in zones)
        {
            dbContext.StoredZones.Add(new StoredZone
            {
                Id = zone.Id,
                Name = zone.Name,
                IsLastActive = zone.Id == activeZoneId
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public static class CloudflareSettingsViewModelMask
{
    public const string MaskedToken = "••••••••••••••••••••";
}
