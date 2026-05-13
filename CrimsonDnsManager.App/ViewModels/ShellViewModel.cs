using CommunityToolkit.Mvvm.ComponentModel;
using CrimsonDnsManager.App.Models;

namespace CrimsonDnsManager.App.ViewModels;

public sealed partial class ShellViewModel : ObservableObject
{
    public ShellViewModel(
        CloudflareSettingsViewModel settings,
        DnsRecordsViewModel dnsRecords,
        UpdateViewModel update)
    {
        Settings = settings;
        DnsRecords = dnsRecords;
        Update = update;

        Settings.ZoneSelected += async (_, zone) =>
        {
            await DnsRecords.SetActiveZoneAsync(zone);
        };
    }

    public CloudflareSettingsViewModel Settings { get; }
    public DnsRecordsViewModel DnsRecords { get; }
    public UpdateViewModel Update { get; }

    [ObservableProperty]
    private string versionText = "DNS MANAGER 0.1.0";
}
