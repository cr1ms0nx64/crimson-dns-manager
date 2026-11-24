using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CrimsonDnsManager.App.Models;
using CrimsonDnsManager.App.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;

namespace CrimsonDnsManager.App.ViewModels;

public sealed partial class DnsRecordsViewModel : ObservableObject
{
    private readonly DnsRecordService _dnsRecordService;
    private readonly LivefeedService _livefeed;
    private readonly CloudflareSettingsViewModel _settings;
    private CancellationTokenSource? _cts;
    private CloudflareZone? _pendingZone;

    public DnsRecordsViewModel(
        DnsRecordService dnsRecordService,
        LivefeedService livefeed,
        CloudflareSettingsViewModel settings)
    {
        _dnsRecordService = dnsRecordService;
        _livefeed = livefeed;
        _settings = settings;
        LivefeedEntries = livefeed.Entries;
        FilterTypes = ["Alle", "A", "AAAA", "CNAME", "MX", "TXT", "SRV"];
    }

    public ObservableCollection<DnsRecord> Records { get; } = [];
    public ObservableCollection<DnsRecord> FilteredRecords { get; } = [];
    public ObservableCollection<LivefeedEntry> LivefeedEntries { get; }
    public IReadOnlyList<string> FilterTypes { get; }

    [ObservableProperty]
    private CloudflareZone? activeZone;

    [ObservableProperty]
    private DnsRecord? selectedRecord;

    [ObservableProperty]
    private DnsRecord? editingRecord;

    [ObservableProperty]
    private string filterType = "Alle";

    [ObservableProperty]
    private string searchQuery = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private bool isAddRecordOpen;

    [ObservableProperty]
    private bool isDeleteModalOpen;

    [ObservableProperty]
    private bool isEditRecordOpen;

    [ObservableProperty]
    private DnsRecord draftRecord = new();

    public bool HasRecords => Records.Count > 0;
    public bool HasSelection => Records.Any(x => x.IsSelected);
    public IEnumerable<DnsRecord> SelectedRecords => Records.Where(x => x.IsSelected);
    public int SelectedCount => Records.Count(x => x.IsSelected);
    public string ActiveZoneName => ActiveZone?.Name ?? _settings.Domain;
    public string PageKicker => $"Cloudflare DNS · {ActiveZoneName}";
    public int ACount => Records.Count(x => x.Type == "A");
    public int AaaaCount => Records.Count(x => x.Type == "AAAA");
    public int TxtCount => Records.Count(x => x.Type == "TXT");
    public int MxCount => Records.Count(x => x.Type == "MX");
    public int SrvCount => Records.Count(x => x.Type == "SRV");
    public string ACountText => ACount.ToString(CultureInfo.InvariantCulture);
    public string AaaaCountText => AaaaCount.ToString(CultureInfo.InvariantCulture);
    public string TxtCountText => TxtCount.ToString(CultureInfo.InvariantCulture);
    public string MxCountText => MxCount.ToString(CultureInfo.InvariantCulture);
    public string SrvCountText => SrvCount.ToString(CultureInfo.InvariantCulture);
    public string LoadedBadgeText => HasRecords ? $"{Records.Count} Records geladen" : "Keine Records";
    public string LoadedBadgeVariant => HasRecords ? "ok" : "gray";

    partial void OnIsSavingChanged(bool value)
    {
        if (!value && _pendingZone is { } zone)
        {
            _pendingZone = null;
            _ = SetActiveZoneAsync(zone);
        }
    }

    partial void OnFilterTypeChanged(string value) => ApplyFilter();
    partial void OnSearchQueryChanged(string value) => ApplyFilter();

    public async Task SetActiveZoneAsync(CloudflareZone zone)
    {
        if (IsSaving) { _pendingZone = zone; return; }
        if (ActiveZone?.Id != zone.Id)
        {
            DetachRecords();
            Records.Clear();
            ApplyFilter();
            NotifyCounts();
            NotifySelection();
            CloseEditRecord();
            IsAddRecordOpen = false;
            IsDeleteModalOpen = false;
        }
        ActiveZone = zone;
        OnPropertyChanged(nameof(ActiveZoneName));
        OnPropertyChanged(nameof(PageKicker));
        await LoadRecordsAsync();
    }

    [RelayCommand]
    private async Task LoadRecordsAsync()
    {
        var zoneId = ActiveZone?.Id ?? _settings.ZoneId;
        if (string.IsNullOrWhiteSpace(zoneId))
        {
            _livefeed.Warning("Keine aktive Zone gesetzt.");
            return;
        }

        if (IsSaving) return;
        _cts?.Cancel();
        using var request = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        _cts = request;
        IsLoading = true;

        try
        {
            var records = await _dnsRecordService.LoadRecordsAsync(zoneId, request.Token);
            if (!ReferenceEquals(_cts, request) || request.IsCancellationRequested) return;
            DetachRecords();
            Records.Clear();
            foreach (var record in records.OrderBy(x => x.Type).ThenBy(x => x.Name))
            {
                record.PropertyChanged += OnRecordPropertyChanged;
                Records.Add(record);
            }

            ApplyFilter();
            NotifyCounts();
            NotifySelection();
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested)
        {
            if (ReferenceEquals(_cts, request)) _livefeed.Warning("Laden der DNS Records abgebrochen oder Zeitlimit erreicht.");
        }
        catch (Exception ex)
        {
            if (!ReferenceEquals(_cts, request)) return;
            _livefeed.Error($"DNS Records konnten nicht geladen werden: {ex.Message}");
        }
        finally
        {
            if (ReferenceEquals(_cts, request)) { _cts = null; IsLoading = false; }
        }
    }

    [RelayCommand]
    private void OpenAddRecord()
    {
        if (IsSaving || IsLoading) return;
        DraftRecord = new DnsRecord
        {
            Type = "A",
            Name = string.Empty,
            Content = string.Empty,
            Ttl = 1,
            Proxied = _settings.DefaultWebRecordsProxied
        };
        IsAddRecordOpen = true;
    }

    [RelayCommand]
    private void CloseAddRecord() => IsAddRecordOpen = false;

    [RelayCommand]
    private async Task CreateRecordAsync()
    {
        if (IsSaving || IsLoading) return;
        var zoneId = ActiveZone?.Id ?? _settings.ZoneId;
        if (string.IsNullOrWhiteSpace(zoneId))
        {
            _livefeed.Warning("Keine aktive Zone gesetzt.");
            return;
        }

        IsSaving = true;
        try
        {
            var created = await _dnsRecordService.CreateAsync(zoneId, DraftRecord.Clone());
            created.PropertyChanged += OnRecordPropertyChanged;
            Records.Add(created);
            IsAddRecordOpen = false;
            ApplyFilter();
            NotifyCounts();
        }
        catch (Exception ex)
        {
            _livefeed.Error($"Record konnte nicht erstellt werden: {ex.Message}");
        }
        finally { IsSaving = false; }
    }

    [RelayCommand]
    private void MarkDirty(DnsRecord? record)
    {
        if (record is null)
        {
            return;
        }

        record.IsDirty = true;
    }

    [RelayCommand]
    private void OpenEditRecord(DnsRecord? record)
    {
        if (IsSaving || IsLoading) return;
        if (record is null)
        {
            return;
        }

        EditingRecord = record;
        IsEditRecordOpen = true;
    }

    [RelayCommand]
    private void CloseEditRecord()
    {
        IsEditRecordOpen = false;
        EditingRecord = null;
    }

    [RelayCommand]
    private async Task SaveEditedRecordAsync(DnsRecord? edited)
    {
        if (IsSaving || IsLoading) return;
        var zoneId = ActiveZone?.Id ?? _settings.ZoneId;
        if (string.IsNullOrWhiteSpace(zoneId))
        {
            _livefeed.Warning("Keine aktive Zone gesetzt.");
            return;
        }

        if (EditingRecord is null || edited is null)
        {
            return;
        }

        var target = EditingRecord;
        IsSaving = true;
        try
        {
            var original = target.Clone();
            var saved = await _dnsRecordService.PatchAsync(zoneId, original, edited);
            ApplyRecordSnapshot(target, saved);
            IsEditRecordOpen = false;
            EditingRecord = null;
            ApplyFilter();
            NotifyCounts();
        }
        catch (Exception ex)
        {
            _livefeed.Error($"Record konnte nicht bearbeitet werden: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task SaveDnsAsync()
    {
        if (IsSaving || IsLoading) return;
        var zoneId = ActiveZone?.Id ?? _settings.ZoneId;
        if (string.IsNullOrWhiteSpace(zoneId))
        {
            _livefeed.Warning("Keine aktive Zone gesetzt.");
            return;
        }

        IsSaving = true;
        try
        {
            await _dnsRecordService.SaveDirtyAsync(zoneId, Records, _settings.BackupBeforeSave);
        }
        catch (Exception ex)
        {
            _livefeed.Error($"DNS speichern fehlgeschlagen: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        try
        {
            await _dnsRecordService.CreateBackupAsync(Records);
        }
        catch (Exception ex)
        {
            _livefeed.Error($"Backup fehlgeschlagen: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenDeleteModal()
    {
        if (!HasSelection)
        {
            return;
        }

        IsDeleteModalOpen = true;
    }

    [RelayCommand]
    private void CloseDeleteModal() => IsDeleteModalOpen = false;

    [RelayCommand]
    private void SelectRecordForDelete(DnsRecord? record)
    {
        if (record is null)
        {
            return;
        }

        foreach (var item in Records)
        {
            item.IsSelected = false;
        }

        record.IsSelected = true;
        IsDeleteModalOpen = true;
        NotifySelection();
    }

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var record in Records)
        {
            record.IsSelected = false;
        }

        NotifySelection();
    }

    [RelayCommand]
    private async Task ConfirmDeleteAsync()
    {
        if (IsSaving || IsLoading) return;
        var zoneId = ActiveZone?.Id ?? _settings.ZoneId;
        if (string.IsNullOrWhiteSpace(zoneId))
        {
            _livefeed.Warning("Keine aktive Zone gesetzt.");
            return;
        }

        var selected = Records.Where(x => x.IsSelected).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        IsSaving = true;
        try
        {
            foreach (var record in selected)
            {
                await _dnsRecordService.DeleteAsync(zoneId, record);
                record.PropertyChanged -= OnRecordPropertyChanged;
                Records.Remove(record);
            }

            IsDeleteModalOpen = false;
        }
        catch (Exception ex)
        {
            _livefeed.Error($"Records konnten nicht gelöscht werden: {ex.Message}");
        }
        finally
        {
            ApplyFilter();
            NotifyCounts();
            NotifySelection();
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void SetFilter(string? type)
    {
        FilterType = string.IsNullOrWhiteSpace(type) ? "Alle" : type;
    }

    [RelayCommand]
    private void ClearLivefeed() => _livefeed.Clear();

    private void ApplyFilter()
    {
        var q = SearchQuery?.Trim().ToLowerInvariant() ?? string.Empty;
        var filtered = Records.Where(record =>
        {
            var typeOk = FilterType == "Alle" || record.Type == FilterType;
            var searchOk = string.IsNullOrWhiteSpace(q)
                || record.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                || record.Type.Contains(q, StringComparison.OrdinalIgnoreCase)
                || record.Content.Contains(q, StringComparison.OrdinalIgnoreCase);
            return typeOk && searchOk;
        }).ToList();

        FilteredRecords.Clear();
        foreach (var record in filtered)
        {
            FilteredRecords.Add(record);
        }

        OnPropertyChanged(nameof(HasRecords));
        OnPropertyChanged(nameof(LoadedBadgeText));
        OnPropertyChanged(nameof(LoadedBadgeVariant));
    }

    private void OnRecordPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DnsRecord.IsSelected))
        {
            NotifySelection();
            return;
        }

        if (sender is DnsRecord record && e.PropertyName is nameof(DnsRecord.Type) or nameof(DnsRecord.Name)
            or nameof(DnsRecord.Content) or nameof(DnsRecord.Ttl) or nameof(DnsRecord.Priority) or nameof(DnsRecord.Proxied))
        {
            record.IsDirty = true;
            ApplyFilter();
            NotifyCounts();
        }
    }

    private void DetachRecords()
    {
        foreach (var record in Records) record.PropertyChanged -= OnRecordPropertyChanged;
    }

    private void NotifyCounts()
    {
        OnPropertyChanged(nameof(HasRecords));
        OnPropertyChanged(nameof(LoadedBadgeText));
        OnPropertyChanged(nameof(LoadedBadgeVariant));
        OnPropertyChanged(nameof(ACount));
        OnPropertyChanged(nameof(AaaaCount));
        OnPropertyChanged(nameof(TxtCount));
        OnPropertyChanged(nameof(MxCount));
        OnPropertyChanged(nameof(SrvCount));
        OnPropertyChanged(nameof(ACountText));
        OnPropertyChanged(nameof(AaaaCountText));
        OnPropertyChanged(nameof(TxtCountText));
        OnPropertyChanged(nameof(MxCountText));
        OnPropertyChanged(nameof(SrvCountText));
    }

    private void NotifySelection()
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(SelectedRecords));
        OnPropertyChanged(nameof(SelectedCount));
    }

    private void ApplyRecordSnapshot(DnsRecord target, DnsRecord source)
    {
        target.PropertyChanged -= OnRecordPropertyChanged;
        target.Id = source.Id;
        target.Type = source.Type;
        target.Name = source.Name;
        target.Content = source.Content;
        target.Ttl = source.Ttl;
        target.Priority = source.Priority;
        target.Proxied = source.Proxied;
        target.IsDirty = false;
        target.PropertyChanged += OnRecordPropertyChanged;
    }
}
