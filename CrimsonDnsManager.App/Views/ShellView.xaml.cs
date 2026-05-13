using CrimsonDnsManager.App.Services;
using CrimsonDnsManager.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace CrimsonDnsManager.App.Views;

public sealed partial class ShellView : UserControl
{
    private bool _updatingTokenMask;
    private bool _showingSavedTokenMask;

    public ShellView()
    {
        InitializeComponent();
        DataContext = App.GetService<ShellViewModel>();
        Loaded += (_, _) =>
        {
            var vm = (ShellViewModel)DataContext;
            if (vm.Settings.ApiToken == CloudflareSettingsViewModelMask.MaskedToken)
            {
                _updatingTokenMask = true;
                _showingSavedTokenMask = true;
                ApiTokenBox.Password = CloudflareSettingsViewModelMask.MaskedToken;
                _updatingTokenMask = false;
            }
        };
    }

    private void ApiTokenBox_OnPasswordChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_updatingTokenMask)
        {
            return;
        }

        if (DataContext is ShellViewModel vm && sender is PasswordBox box && !string.IsNullOrWhiteSpace(box.Password))
        {
            _showingSavedTokenMask = false;
            vm.Settings.ApiToken = box.Password;
        }
    }

    private void ApiTokenBox_OnGotFocus(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (!_showingSavedTokenMask || sender is not PasswordBox box)
        {
            return;
        }

        _updatingTokenMask = true;
        box.Password = string.Empty;
        _updatingTokenMask = false;
        _showingSavedTokenMask = false;
    }
}
