using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections;
using System.Windows.Input;

namespace CrimsonDnsManager.App.Controls;

public sealed partial class DeleteConfirmDialog : UserControl
{
    public static readonly DependencyProperty RecordsProperty =
        DependencyProperty.Register(
            nameof(Records),
            typeof(IEnumerable),
            typeof(DeleteConfirmDialog),
            new PropertyMetadata(null));

    public static readonly DependencyProperty CancelCommandProperty =
        DependencyProperty.Register(
            nameof(CancelCommand),
            typeof(ICommand),
            typeof(DeleteConfirmDialog),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ConfirmCommandProperty =
        DependencyProperty.Register(
            nameof(ConfirmCommand),
            typeof(ICommand),
            typeof(DeleteConfirmDialog),
            new PropertyMetadata(null));

    public DeleteConfirmDialog()
    {
        InitializeComponent();
    }

    public IEnumerable? Records
    {
        get => (IEnumerable?)GetValue(RecordsProperty);
        set => SetValue(RecordsProperty, value);
    }

    public ICommand? CancelCommand
    {
        get => (ICommand?)GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    public ICommand? ConfirmCommand
    {
        get => (ICommand?)GetValue(ConfirmCommandProperty);
        set => SetValue(ConfirmCommandProperty, value);
    }
}
