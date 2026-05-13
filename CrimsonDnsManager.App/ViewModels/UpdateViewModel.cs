using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CrimsonDnsManager.App.Services;
using Windows.Storage.Pickers;

namespace CrimsonDnsManager.App.ViewModels;

public sealed partial class UpdateViewModel : ObservableObject
{
    private readonly UpdateService _updateService;
    private readonly LivefeedService _livefeed;
    private readonly WindowHandleProvider _windowHandleProvider;

    public UpdateViewModel(UpdateService updateService, LivefeedService livefeed, WindowHandleProvider windowHandleProvider)
    {
        _updateService = updateService;
        _livefeed = livefeed;
        _windowHandleProvider = windowHandleProvider;
        StatusText = _updateService.Current.StatusText;
    }

    [ObservableProperty]
    private string statusText;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isVerified;

    public string BadgeText => IsVerified ? "bereit" : "kein Update";

    [RelayCommand]
    private async Task PickPackageAsync()
    {
        try
        {
            var picker = new FileOpenPicker();
            var hwnd = _windowHandleProvider.GetHandle();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.FileTypeFilter.Add(".zip");
            picker.FileTypeFilter.Add(".msix");
            picker.FileTypeFilter.Add(".msixbundle");

            var file = await picker.PickSingleFileAsync();
            if (file is null)
            {
                _livefeed.Info("Update Paket Auswahl abgebrochen.");
                return;
            }

            await _updateService.SetPackageAsync(file.Path);
            RefreshState();
        }
        catch (Exception ex)
        {
            _livefeed.Error($"Update Paket konnte nicht gewählt werden: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task VerifyPackageAsync()
    {
        IsBusy = true;
        try
        {
            await _updateService.VerifyAsync();
            RefreshState();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task InstallAndRestartAsync()
    {
        IsBusy = true;
        try
        {
            await _updateService.InstallAndRestartAsync();
            RefreshState();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RefreshState()
    {
        StatusText = _updateService.Current.StatusText;
        IsVerified = _updateService.Current.IsVerified;
        OnPropertyChanged(nameof(BadgeText));
    }
}
