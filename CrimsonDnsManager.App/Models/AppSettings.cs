namespace CrimsonDnsManager.App.Models;

public sealed class AppSettings
{
    public int Id { get; set; } = 1;
    public string Domain { get; set; } = string.Empty;
    public string ZoneId { get; set; } = string.Empty;
    public string? AccountId { get; set; }
    public bool DefaultWebRecordsProxied { get; set; } = true;
    public bool BackupBeforeSave { get; set; } = true;
    public string? LastActiveZoneId { get; set; }
    public string? LastActiveZoneName { get; set; }
}
