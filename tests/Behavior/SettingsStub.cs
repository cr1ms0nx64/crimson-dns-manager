namespace CrimsonDnsManager.App.ViewModels;

// Replaces Windows-only settings persistence; all DNS logic uses production code.
public sealed class CloudflareSettingsViewModel
{
    public string ZoneId { get; set; } = "zone-one";
    public string Domain { get; set; } = "example.test";
    public bool DefaultWebRecordsProxied { get; set; }
    public bool BackupBeforeSave => false;
}
