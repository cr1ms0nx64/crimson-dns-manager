using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CrimsonDnsManager.App.Data;
using CrimsonDnsManager.App.Models;
using CrimsonDnsManager.App.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace CrimsonDnsManager.App.ViewModels;

public sealed partial class CloudflareSettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly CloudflareApiService _cloudflareApi;
    private readonly ISecretService _secretService;
    private readonly LivefeedService _livefeed;
    private CancellationTokenSource? _cts;

    public CloudflareSettingsViewModel(
        SettingsService settingsService,
        CloudflareApiService cloudflareApi,
        ISecretService secretService,
        LivefeedService livefeed)
    {
        _settingsService = settingsService;
        _cloudflareApi = cloudflareApi;
        _secretService = secretService;
        _livefeed = livefeed;
        _ = LoadSettingsAsync();
    }

    public event EventHandler<CloudflareZone>? ZoneSelected;

    public ObservableCollection<CloudflareZone> Zones { get; } = [];

    [ObservableProperty]
    private string apiToken = string.Empty;

    [ObservableProperty]
    private string domain = "cr1ms0n.xyz";

    [ObservableProperty]
    private string zoneId = string.Empty;

    [ObservableProperty]
    private string? accountId;

    [ObservableProperty]
    private bool defaultWebRecordsProxied = true;

    [ObservableProperty]
    private bool backupBeforeSave = true;

    [ObservableProperty]
    private bool isLoadingZones;

    [ObservableProperty]
    private bool isTestingConnection;

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private CloudflareZone? selectedZone;

    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        try
        {
            var settings = await _settingsService.LoadAsync();
            Domain = settings.Domain;
            ZoneId = settings.ZoneId;
            AccountId = settings.AccountId;
            DefaultWebRecordsProxied = settings.DefaultWebRecordsProxied;
            BackupBeforeSave = settings.BackupBeforeSave;
            ApiToken = await _secretService.HasCloudflareTokenAsync()
                ? CloudflareSettingsViewModelMask.MaskedToken
                : string.Empty;

            if (!string.IsNullOrWhiteSpace(settings.LastActiveZoneId) && !string.IsNullOrWhiteSpace(settings.LastActiveZoneName))
            {
                var zone = new CloudflareZone
                {
                    Id = settings.LastActiveZoneId,
                    Name = settings.LastActiveZoneName,
                    IsActive = true
                };
                Zones.Add(zone);
                SelectedZone = zone;
            }

            _livefeed.Info("App gestartet.");
        }
        catch (Exception ex)
        {
            _livefeed.Error($"Einstellungen konnten nicht geladen werden: {ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        IsSaving = true;
        try
        {
            var settings = new AppSettings
            {
                Domain = Domain,
                ZoneId = ZoneId,
                AccountId = AccountId,
                DefaultWebRecordsProxied = DefaultWebRecordsProxied,
                BackupBeforeSave = BackupBeforeSave,
                LastActiveZoneId = SelectedZone?.Id,
                LastActiveZoneName = SelectedZone?.Name
            };

            await _settingsService.SaveAsync(settings, ApiToken);
        }
        catch (Exception ex)
        {
            _livefeed.Error($"Einstellungen konnten nicht gespeichert werden: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanSave() => !IsSaving;

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        IsTestingConnection = true;
        try
        {
            await SaveAsync();
            await _cloudflareApi.TestConnectionAsync();
        }
        catch (Exception ex)
        {
            _livefeed.Error($"Verbindungstest fehlgeschlagen: {ex.Message}");
        }
        finally
        {
            IsTestingConnection = false;
        }
    }

    [RelayCommand]
    private async Task LoadZonesAsync()
    {
        IsLoadingZones = true;
        _cts?.Cancel();
        _cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        try
        {
            await SaveAsync();
            var zones = await _cloudflareApi.GetZonesAsync(Domain, _cts.Token);
            Zones.Clear();

            foreach (var zone in zones)
            {
                zone.IsActive = zone.Id == ZoneId || zone.Id == SelectedZone?.Id;
                Zones.Add(zone);
            }

            if (Zones.Count > 0 && SelectedZone is null)
            {
                SelectZone(Zones[0]);
            }

            await _settingsService.StoreZonesAsync(Zones, SelectedZone?.Id, _cts.Token);
        }
        catch (Exception ex)
        {
            _livefeed.Error($"Zonen konnten nicht geladen werden: {ex.Message}");
        }
        finally
        {
            IsLoadingZones = false;
        }
    }

    [RelayCommand]
    private void SelectZone(CloudflareZone? zone)
    {
        if (zone is null)
        {
            return;
        }

        foreach (var item in Zones)
        {
            item.IsActive = item.Id == zone.Id;
        }

        SelectedZone = zone;
        ZoneId = zone.Id;
        Domain = zone.Name;
        _livefeed.Success($"Zone aktiv: {zone.Name}");
        ZoneSelected?.Invoke(this, zone);
    }
    [RelayCommand]
    private void CopyZoneId()
    {
        if (string.IsNullOrWhiteSpace(ZoneId))
        {
            _livefeed.Warning("Keine Zone ID zum Kopieren vorhanden.");
            return;
        }

        var data = new DataPackage();
        data.SetText(ZoneId);
        Clipboard.SetContent(data);
        _livefeed.Success("Zone ID kopiert.");
    }

    [RelayCommand]
    private async Task OpenDashboardAsync()
    {
        var target = string.IsNullOrWhiteSpace(ZoneId)
            ? "https://dash.cloudflare.com/"
            : $"https://dash.cloudflare.com/?to=/:account/:zone/dns/records";

        await Launcher.LaunchUriAsync(new Uri(target));
        _livefeed.Info("Cloudflare Dashboard geöffnet.");
    }

    [RelayCommand]
    private void PrepareHubRecord()
    {
        _livefeed.Info("Hub Record Vorlage bereit. Name next, Typ A, Proxy aktiv.");
    }

    [RelayCommand]
    private void OpenBackupFolder()
    {
        try
        {
            AppPaths.EnsureCreated();
            Process.Start(new ProcessStartInfo
            {
                FileName = AppPaths.BackupPath,
                UseShellExecute = true
            });
            _livefeed.Info("Backup Ordner geöffnet.");
        }
        catch (Exception ex)
        {
            _livefeed.Error($"Backup Ordner konnte nicht geöffnet werden: {ex.Message}");
        }
    }
}
