using CrimsonDnsManager.App.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.ComponentModel;

namespace CrimsonDnsManager.App.Controls;

public sealed partial class DnsRecordRow : UserControl
{
    public static readonly DependencyProperty RecordProperty =
        DependencyProperty.Register(
            nameof(Record),
            typeof(DnsRecord),
            typeof(DnsRecordRow),
            new PropertyMetadata(new DnsRecord(), OnRecordChanged));

    private bool _isPointerOver;
    private DnsRecord? _attachedRecord;

    public DnsRecordRow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            AttachRecord(Record);
            ApplyRecordVisuals();
        };
        Unloaded += (_, _) => DetachRecord(_attachedRecord);
    }

    public DnsRecord Record
    {
        get => (DnsRecord)GetValue(RecordProperty);
        set => SetValue(RecordProperty, value);
    }

    public event EventHandler<DnsRecordEventArgs>? EditRequested;
    public event EventHandler<DnsRecordEventArgs>? DeleteRequested;
    public event EventHandler<DnsRecordEventArgs>? SelectionChanged;

    private static void OnRecordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var row = (DnsRecordRow)d;
        row.DetachRecord(e.OldValue as DnsRecord);
        row.AttachRecord(e.NewValue as DnsRecord);
        row.Bindings.Update();
        row.ApplyRecordVisuals();
    }

    private void AttachRecord(DnsRecord? record)
    {
        if (record is null || ReferenceEquals(_attachedRecord, record))
        {
            return;
        }

        DetachRecord(_attachedRecord);
        record.PropertyChanged += Record_PropertyChanged;
        _attachedRecord = record;
    }

    private void DetachRecord(DnsRecord? record)
    {
        if (record is not null && ReferenceEquals(_attachedRecord, record))
        {
            record.PropertyChanged -= Record_PropertyChanged;
            _attachedRecord = null;
        }
    }

    private void Record_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DnsRecord.Type)
            or nameof(DnsRecord.Ttl)
            or nameof(DnsRecord.Priority)
            or nameof(DnsRecord.Proxied)
            or nameof(DnsRecord.IsSelected))
        {
            ApplyRecordVisuals();
        }
    }

    private void ApplyRecordVisuals()
    {
        ApplyTypeColor();
        ApplyTtl();
        ApplyPriority();
        ApplyProxy();
        ApplySelectionVisual();
    }

    private void ApplyTypeColor()
    {
        TypeText.Foreground = Record.Type switch
        {
            "A" => new SolidColorBrush(ColorHelper.FromArgb(255, 64, 200, 244)),
            "AAAA" => new SolidColorBrush(ColorHelper.FromArgb(255, 120, 220, 255)),
            "CNAME" => new SolidColorBrush(ColorHelper.FromArgb(255, 244, 192, 64)),
            "MX" => new SolidColorBrush(ColorHelper.FromArgb(255, 167, 139, 250)),
            "TXT" => new SolidColorBrush(ColorHelper.FromArgb(255, 52, 212, 138)),
            "SRV" => new SolidColorBrush(ColorHelper.FromArgb(255, 251, 146, 60)),
            _ => new SolidColorBrush(ColorHelper.FromArgb(255, 154, 184, 204))
        };
    }

    private void ApplyTtl()
    {
        TtlText.Text = Record.Ttl == 1 ? "Auto" : Record.Ttl.ToString();
    }

    private void ApplyPriority()
    {
        PriorityText.Text = Record.Priority.HasValue ? Record.Priority.Value.ToString() : "—";
    }

    private void ApplyProxy()
    {
        ProxyIcon.Text = Record.Proxied ? "◈" : "○";
        ProxyIcon.Foreground = Record.Proxied
            ? new SolidColorBrush(ColorHelper.FromArgb(255, 244, 192, 64))
            : new SolidColorBrush(ColorHelper.FromArgb(102, 90, 122, 150));
    }

    private void RowGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOver = true;
        if (Record.IsSelected)
        {
            RowGrid.Background = new SolidColorBrush(ColorHelper.FromArgb(23, 244, 80, 96));
            SelectionRail.BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(153, 244, 80, 96));
            return;
        }

        RowGrid.Background = new SolidColorBrush(ColorHelper.FromArgb(13, 64, 200, 244));
        SelectionRail.BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(128, 64, 200, 244));
    }

    private void RowGrid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOver = false;
        ApplySelectionVisual();
    }

    private void ApplySelectionVisual()
    {
        if (_isPointerOver)
        {
            RowGrid_PointerEntered(RowGrid, null!);
            return;
        }

        if (Record.IsSelected)
        {
            RowGrid.Background = new SolidColorBrush(ColorHelper.FromArgb(15, 244, 80, 96));
            SelectionRail.BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(153, 244, 80, 96));
            return;
        }

        RowGrid.Background = new SolidColorBrush(Colors.Transparent);
        SelectionRail.BorderBrush = new SolidColorBrush(Colors.Transparent);
    }

    private void Checkbox_Changed(object sender, RoutedEventArgs e)
    {
        ApplySelectionVisual();
        SelectionChanged?.Invoke(this, new DnsRecordEventArgs(Record));
    }

    private void EditBtn_Click(object sender, RoutedEventArgs e)
    {
        EditRequested?.Invoke(this, new DnsRecordEventArgs(Record));
    }

    private void DeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        DeleteRequested?.Invoke(this, new DnsRecordEventArgs(Record));
    }

    private void EditButton_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        EditButtonText.Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 221, 238, 255));
    }

    private void DeleteButton_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        DeleteButtonText.Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 244, 80, 96));
    }

    private void RowActionButton_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var muted = new SolidColorBrush(ColorHelper.FromArgb(255, 90, 122, 150));
        EditButtonText.Foreground = muted;
        DeleteButtonText.Foreground = muted;
    }
}

public sealed class DnsRecordEventArgs(DnsRecord record) : EventArgs
{
    public DnsRecord Record { get; } = record;
}
