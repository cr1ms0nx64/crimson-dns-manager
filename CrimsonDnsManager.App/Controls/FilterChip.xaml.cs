using System.Windows.Input;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace CrimsonDnsManager.App.Controls;

public sealed partial class FilterChip : UserControl
{
    private bool _isHovered;

    public FilterChip()
    {
        InitializeComponent();
        ApplyState();
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(FilterChip), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive), typeof(bool), typeof(FilterChip), new PropertyMetadata(false, OnVisualStateChanged));

    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command), typeof(ICommand), typeof(FilterChip), new PropertyMetadata(null));

    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
        nameof(CommandParameter), typeof(object), typeof(FilterChip), new PropertyMetadata(null));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    private static void OnVisualStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FilterChip chip)
        {
            chip.ApplyState();
        }
    }

    private void ChipRoot_OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isHovered = true;
        ApplyState();
    }

    private void ChipRoot_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isHovered = false;
        ApplyState();
    }

    private void ChipRoot_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        ExecuteCommand();
    }

    private void Root_OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is Windows.System.VirtualKey.Enter or Windows.System.VirtualKey.Space)
        {
            ExecuteCommand();
            e.Handled = true;
        }
    }

    private void ApplyState()
    {
        if (ChipRoot is null || ChipText is null)
        {
            return;
        }

        var highlighted = IsActive || _isHovered;
        ChipRoot.Background = new SolidColorBrush(highlighted
            ? ColorHelper.FromArgb(23, 64, 200, 244)
            : ColorHelper.FromArgb(0, 0, 0, 0));
        ChipRoot.BorderBrush = new SolidColorBrush(highlighted
            ? ColorHelper.FromArgb(46, 120, 154, 200)
            : ColorHelper.FromArgb(20, 120, 154, 200));
        ChipText.Foreground = new SolidColorBrush(IsActive
            ? ColorHelper.FromArgb(255, 64, 200, 244)
            : highlighted
                ? ColorHelper.FromArgb(255, 221, 238, 255)
                : ColorHelper.FromArgb(255, 90, 122, 150));
    }

    private void ExecuteCommand()
    {
        if (Command?.CanExecute(CommandParameter) == true)
        {
            Command.Execute(CommandParameter);
        }
    }
}
