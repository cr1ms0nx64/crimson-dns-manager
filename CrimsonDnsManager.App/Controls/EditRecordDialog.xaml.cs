using CrimsonDnsManager.App.Models;
using CrimsonDnsManager.App.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Globalization;
using System.Windows.Input;

namespace CrimsonDnsManager.App.Controls;

public sealed partial class EditRecordDialog : UserControl
{
    public static readonly DependencyProperty RecordProperty =
        DependencyProperty.Register(
            nameof(Record),
            typeof(DnsRecord),
            typeof(EditRecordDialog),
            new PropertyMetadata(null, OnRecordChanged));

    public static readonly DependencyProperty DomainProperty =
        DependencyProperty.Register(
            nameof(Domain),
            typeof(string),
            typeof(EditRecordDialog),
            new PropertyMetadata(string.Empty, OnDomainChanged));

    public static readonly DependencyProperty SaveCommandProperty =
        DependencyProperty.Register(
            nameof(SaveCommand),
            typeof(ICommand),
            typeof(EditRecordDialog),
            new PropertyMetadata(null));

    public static readonly DependencyProperty CancelCommandProperty =
        DependencyProperty.Register(
            nameof(CancelCommand),
            typeof(ICommand),
            typeof(EditRecordDialog),
            new PropertyMetadata(null));

    private bool _loading;

    public EditRecordDialog()
    {
        InitializeComponent();
    }

    public DnsRecord? Record
    {
        get => (DnsRecord?)GetValue(RecordProperty);
        set => SetValue(RecordProperty, value);
    }

    public string Domain
    {
        get => (string)GetValue(DomainProperty);
        set => SetValue(DomainProperty, value);
    }

    public ICommand? SaveCommand
    {
        get => (ICommand?)GetValue(SaveCommandProperty);
        set => SetValue(SaveCommandProperty, value);
    }

    public ICommand? CancelCommand
    {
        get => (ICommand?)GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    private static void OnRecordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((EditRecordDialog)d).LoadRecord(e.NewValue as DnsRecord);
    }

    private static void OnDomainChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((EditRecordDialog)d).UpdateHeader();
    }

    private void LoadRecord(DnsRecord? record)
    {
        _loading = true;
        ValidationError.Visibility = Visibility.Collapsed;

        if (record is null)
        {
            DialogTitle.Text = "Record bearbeiten";
            DialogSub.Text = string.Empty;
            TypeCombo.SelectedItem = "A";
            NameBox.Text = string.Empty;
            ContentBox.Text = string.Empty;
            TtlBox.Text = string.Empty;
            PrioBox.Text = string.Empty;
            ProxyCheck.IsChecked = false;
            DirtyBanner.Visibility = Visibility.Collapsed;
            _loading = false;
            return;
        }

        TypeCombo.SelectedItem = record.Type;
        NameBox.Text = record.Name;
        ContentBox.Text = record.Content;
        TtlBox.Text = record.Ttl == 1 ? string.Empty : record.Ttl.ToString(CultureInfo.InvariantCulture);
        PrioBox.Text = record.Priority?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        ProxyCheck.IsChecked = record.Proxied;

        ApplyProxyAvailability(record.Type);
        UpdateHeader();
        DirtyBanner.Visibility = Visibility.Collapsed;
        _loading = false;
    }

    private void UpdateHeader()
    {
        if (Record is null)
        {
            return;
        }

        DialogTitle.Text = $"\"{Record.Name}\" bearbeiten";
        DialogSub.Text = $"{Record.Name} · {Record.Type} · {Domain}";
    }

    private void TypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var type = GetSelectedType();
        ApplyProxyAvailability(type);
        if (!_loading)
        {
            MarkDirty();
        }
    }

    private void ApplyProxyAvailability(string type)
    {
        var proxyOk = CanProxy(type);
        ProxyCheck.IsEnabled = proxyOk;
        if (!proxyOk)
        {
            ProxyCheck.IsChecked = false;
        }

        ProxyNote.Text = proxyOk ? string.Empty : $"Proxy nicht verfügbar für {type}-Records.";
        ProxyNote.Visibility = proxyOk ? Visibility.Collapsed : Visibility.Visible;
        ContentBox.PlaceholderText = type switch
        {
            "A" => "IPv4 Adresse",
            "AAAA" => "IPv6 Adresse",
            "CNAME" => "Zieldomain",
            "MX" => "Mailserver",
            "TXT" => "TXT-String",
            "SRV" => "Zieldomain",
            _ => "Content"
        };
    }

    private static bool CanProxy(string type)
    {
        return type is "A" or "AAAA" or "CNAME";
    }

    private string GetSelectedType()
    {
        return TypeCombo.SelectedItem?.ToString() ?? Record?.Type ?? "A";
    }

    private void AnyTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loading)
        {
            MarkDirty();
        }
    }

    private void ProxyCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (!_loading)
        {
            MarkDirty();
        }
    }

    private void MarkDirty()
    {
        DirtyBanner.Visibility = Visibility.Visible;
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e)
    {
        ExecuteCommand(CancelCommand, null);
    }

    private void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        if (Record is null)
        {
            ExecuteCommand(CancelCommand, null);
            return;
        }

        try
        {
            var edited = BuildEditedRecord();
            CloudflareApiService.ValidateRecord(edited);
            ValidationError.Visibility = Visibility.Collapsed;
            ExecuteCommand(SaveCommand, edited);
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException)
        {
            ValidationError.Text = ex.Message;
            ValidationError.Visibility = Visibility.Visible;
        }
    }

    private DnsRecord BuildEditedRecord()
    {
        var type = GetSelectedType();
        var ttl = string.IsNullOrWhiteSpace(TtlBox.Text) ? 1
            : int.TryParse(TtlBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedTtl)
                ? parsedTtl : throw new FormatException("TTL muss eine ganze Zahl sein.");
        var priority = int.TryParse(PrioBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedPriority)
            ? parsedPriority
            : string.IsNullOrWhiteSpace(PrioBox.Text) ? (int?)null : throw new FormatException("Priority muss eine ganze Zahl sein.");

        return new DnsRecord
        {
            Id = Record?.Id,
            Type = type,
            Name = NameBox.Text.Trim(),
            Content = ContentBox.Text.Trim(),
            Ttl = ttl,
            Priority = priority,
            Proxied = CanProxy(type) && ProxyCheck.IsChecked == true
        };
    }

    private static void ExecuteCommand(ICommand? command, object? parameter)
    {
        if (command?.CanExecute(parameter) == true)
        {
            command.Execute(parameter);
        }
    }
}
