using CrimsonDnsManager.App.Data;
using CrimsonDnsManager.App.Models;
using System.Text.Json;

namespace CrimsonDnsManager.App.Services;

public sealed class DnsRecordService(CloudflareApiService cloudflareApi, LivefeedService livefeed)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public Task<IReadOnlyList<DnsRecord>> LoadRecordsAsync(string zoneId, CancellationToken cancellationToken = default)
    {
        return cloudflareApi.GetDnsRecordsAsync(zoneId, cancellationToken);
    }

    public Task<DnsRecord> CreateAsync(string zoneId, DnsRecord record, CancellationToken cancellationToken = default)
    {
        return cloudflareApi.CreateRecordAsync(zoneId, record, cancellationToken);
    }

    public Task<DnsRecord> UpdateAsync(string zoneId, DnsRecord record, CancellationToken cancellationToken = default)
    {
        return cloudflareApi.UpdateRecordAsync(zoneId, record, cancellationToken);
    }

    public Task<DnsRecord> PatchAsync(string zoneId, DnsRecord original, DnsRecord updated, CancellationToken cancellationToken = default)
    {
        return cloudflareApi.PatchRecordAsync(zoneId, original, updated, cancellationToken);
    }

    public Task DeleteAsync(string zoneId, DnsRecord record, CancellationToken cancellationToken = default)
    {
        return cloudflareApi.DeleteRecordAsync(zoneId, record, cancellationToken);
    }

    public async Task SaveDirtyAsync(string zoneId, IEnumerable<DnsRecord> records, bool backupBeforeSave, CancellationToken cancellationToken = default)
    {
        var dirtyRecords = records.Where(x => x.IsDirty).ToList();
        if (dirtyRecords.Count == 0)
        {
            livefeed.Info("Keine DNS Änderungen zum Speichern vorhanden.");
            return;
        }

        if (backupBeforeSave)
        {
            await CreateBackupAsync(records, cancellationToken);
        }

        foreach (var record in dirtyRecords)
        {
            if (string.IsNullOrWhiteSpace(record.Id))
            {
                await cloudflareApi.CreateRecordAsync(zoneId, record, cancellationToken);
            }
            else
            {
                await cloudflareApi.UpdateRecordAsync(zoneId, record, cancellationToken);
            }

            record.IsDirty = false;
        }

        livefeed.Success($"DNS Records gespeichert. Änderungen: {dirtyRecords.Count}");
    }

    public async Task<string> CreateBackupAsync(IEnumerable<DnsRecord> records, CancellationToken cancellationToken = default)
    {
        AppPaths.EnsureCreated();
        var fileName = $"dns_backup_{DateTimeOffset.Now:yyyyMMdd_HHmmss}.json";
        var filePath = Path.Combine(AppPaths.BackupPath, fileName);
        var json = JsonSerializer.Serialize(records.Select(x => x.Clone()), JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
        livefeed.Success($"Backup erstellt: {fileName}");
        return filePath;
    }
}
