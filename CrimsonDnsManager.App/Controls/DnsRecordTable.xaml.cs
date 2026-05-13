using CrimsonDnsManager.App.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;

namespace CrimsonDnsManager.App.Controls;

public sealed partial class DnsRecordTable : UserControl
{
    public static readonly DependencyProperty RecordsProperty =
        DependencyProperty.Register(
            nameof(Records),
            typeof(IEnumerable),
            typeof(DnsRecordTable),
            new PropertyMetadata(null, OnRecordsChanged));

    public static readonly DependencyProperty HasRecordsProperty =
        DependencyProperty.Register(
            nameof(HasRecords),
            typeof(bool),
            typeof(DnsRecordTable),
            new PropertyMetadata(false, OnStateChanged));

    public static readonly DependencyProperty HasSelectionProperty =
        DependencyProperty.Register(
            nameof(HasSelection),
            typeof(bool),
            typeof(DnsRecordTable),
            new PropertyMetadata(false, OnStateChanged));

    public static readonly DependencyProperty SelectedCountProperty =
        DependencyProperty.Register(
            nameof(SelectedCount),
            typeof(int),
            typeof(DnsRecordTable),
            new PropertyMetadata(0, OnStateChanged));

    public static readonly DependencyProperty ClearSelectionCommandProperty =
        DependencyProperty.Register(
            nameof(ClearSelectionCommand),
            typeof(ICommand),
            typeof(DnsRecordTable),
            new PropertyMetadata(null));

    public static readonly DependencyProperty DeleteSelectedCommandProperty =
        DependencyProperty.Register(
            nameof(DeleteSelectedCommand),
            typeof(ICommand),
            typeof(DnsRecordTable),
            new PropertyMetadata(null));

    public static readonly DependencyProperty EditRecordCommandProperty =
        DependencyProperty.Register(
            nameof(EditRecordCommand),
            typeof(ICommand),
            typeof(DnsRecordTable),
            new PropertyMetadata(null));

    public static readonly DependencyProperty DeleteRecordCommandProperty =
        DependencyProperty.Register(
            nameof(DeleteRecordCommand),
            typeof(ICommand),
            typeof(DnsRecordTable),
            new PropertyMetadata(null));

    public static readonly DependencyProperty LoadRecordsCommandProperty =
        DependencyProperty.Register(
            nameof(LoadRecordsCommand),
            typeof(ICommand),
            typeof(DnsRecordTable),
            new PropertyMetadata(null));

    private readonly List<DnsRecord> _subscribedRecords = [];
    private INotifyCollectionChanged? _recordsCollection;
    private bool _updatingHeaderCheckBox;

    public DnsRecordTable()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (_recordsCollection is null && Records is not null)
            {
                AttachRecords(Records);
            }

            UpdateSelectionBar();
            UpdateHeaderCheckBox();
            UpdateEmptyState();
        };
        Unloaded += (_, _) => DetachRecords();
    }

    public IEnumerable? Records
    {
        get => (IEnumerable?)GetValue(RecordsProperty);
        set => SetValue(RecordsProperty, value);
    }

    public bool HasRecords
    {
        get => (bool)GetValue(HasRecordsProperty);
        set => SetValue(HasRecordsProperty, value);
    }

    public bool HasSelection
    {
        get => (bool)GetValue(HasSelectionProperty);
        set => SetValue(HasSelectionProperty, value);
    }

    public int SelectedCount
    {
        get => (int)GetValue(SelectedCountProperty);
        set => SetValue(SelectedCountProperty, value);
    }

    public ICommand? ClearSelectionCommand
    {
        get => (ICommand?)GetValue(ClearSelectionCommandProperty);
        set => SetValue(ClearSelectionCommandProperty, value);
    }

    public ICommand? DeleteSelectedCommand
    {
        get => (ICommand?)GetValue(DeleteSelectedCommandProperty);
        set => SetValue(DeleteSelectedCommandProperty, value);
    }

    public ICommand? EditRecordCommand
    {
        get => (ICommand?)GetValue(EditRecordCommandProperty);
        set => SetValue(EditRecordCommandProperty, value);
    }

    public ICommand? DeleteRecordCommand
    {
        get => (ICommand?)GetValue(DeleteRecordCommandProperty);
        set => SetValue(DeleteRecordCommandProperty, value);
    }

    public ICommand? LoadRecordsCommand
    {
        get => (ICommand?)GetValue(LoadRecordsCommandProperty);
        set => SetValue(LoadRecordsCommandProperty, value);
    }

    private static void OnRecordsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var table = (DnsRecordTable)d;
        table.DetachRecords();
        table.AttachRecords(e.NewValue as IEnumerable);
        table.UpdateHeaderCheckBox();
        table.UpdateEmptyState();
    }

    private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var table = (DnsRecordTable)d;
        table.UpdateSelectionBar();
        table.UpdateHeaderCheckBox();
        table.UpdateEmptyState();
    }

    private void AttachRecords(IEnumerable? records)
    {
        if (records is INotifyCollectionChanged collection)
        {
            _recordsCollection = collection;
            collection.CollectionChanged += Records_CollectionChanged;
        }

        RefreshRecordSubscriptions();
    }

    private void DetachRecords()
    {
        if (_recordsCollection is not null)
        {
            _recordsCollection.CollectionChanged -= Records_CollectionChanged;
            _recordsCollection = null;
        }

        foreach (var record in _subscribedRecords)
        {
            record.PropertyChanged -= Record_PropertyChanged;
        }

        _subscribedRecords.Clear();
    }

    private void RefreshRecordSubscriptions()
    {
        foreach (var record in _subscribedRecords)
        {
            record.PropertyChanged -= Record_PropertyChanged;
        }

        _subscribedRecords.Clear();

        foreach (var record in EnumerateRecords())
        {
            record.PropertyChanged += Record_PropertyChanged;
            _subscribedRecords.Add(record);
        }
    }

    private void Records_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            RefreshRecordSubscriptions();
        }
        else
        {
            if (e.OldItems is not null)
            {
                foreach (var item in e.OldItems.OfType<DnsRecord>())
                {
                    item.PropertyChanged -= Record_PropertyChanged;
                    _subscribedRecords.Remove(item);
                }
            }

            if (e.NewItems is not null)
            {
                foreach (var item in e.NewItems.OfType<DnsRecord>())
                {
                    item.PropertyChanged += Record_PropertyChanged;
                    _subscribedRecords.Add(item);
                }
            }
        }

        UpdateHeaderCheckBox();
        UpdateEmptyState();
    }

    private void Record_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DnsRecord.IsSelected))
        {
            UpdateHeaderCheckBox();
        }
    }

    private IEnumerable<DnsRecord> EnumerateRecords()
    {
        return Records?.OfType<DnsRecord>() ?? [];
    }

    private void UpdateSelectionBar()
    {
        if (SelectionBar is null || SelCountText is null)
        {
            return;
        }

        SelectionBar.Visibility = HasSelection ? Visibility.Visible : Visibility.Collapsed;
        SelCountText.Text = SelectedCount.ToString();
    }

    private void UpdateHeaderCheckBox()
    {
        if (HeaderCheckBox is null)
        {
            return;
        }

        var visible = EnumerateRecords().ToList();
        bool? state = false;
        if (visible.Count > 0)
        {
            var selected = visible.Count(x => x.IsSelected);
            state = selected == 0 ? false : selected == visible.Count ? true : null;
        }

        _updatingHeaderCheckBox = true;
        HeaderCheckBox.IsChecked = state;
        _updatingHeaderCheckBox = false;
    }

    private void UpdateEmptyState()
    {
        if (RecordsList is null || EmptyState is null)
        {
            return;
        }

        RecordsList.Visibility = HasRecords ? Visibility.Visible : Visibility.Collapsed;
        EmptyState.Visibility = HasRecords ? Visibility.Collapsed : Visibility.Visible;
    }

    private void HeaderCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (_updatingHeaderCheckBox)
        {
            return;
        }

        SetVisibleSelection(true);
    }

    private void HeaderCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_updatingHeaderCheckBox)
        {
            return;
        }

        SetVisibleSelection(false);
    }

    private void HeaderCheckBox_Indeterminate(object sender, RoutedEventArgs e)
    {
        if (_updatingHeaderCheckBox)
        {
            return;
        }

        UpdateHeaderCheckBox();
    }

    private void SetVisibleSelection(bool isSelected)
    {
        foreach (var record in EnumerateRecords())
        {
            record.IsSelected = isSelected;
        }

        UpdateHeaderCheckBox();
    }

    private void Row_EditRequested(object sender, DnsRecordEventArgs e)
    {
        ExecuteCommand(EditRecordCommand, e.Record);
    }

    private void Row_DeleteRequested(object sender, DnsRecordEventArgs e)
    {
        ExecuteCommand(DeleteRecordCommand, e.Record);
    }

    private void Row_SelectionChanged(object sender, DnsRecordEventArgs e)
    {
        UpdateHeaderCheckBox();
    }

    private void ClearSelection_Click(object sender, RoutedEventArgs e)
    {
        ExecuteCommand(ClearSelectionCommand, null);
    }

    private void DeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        ExecuteCommand(DeleteSelectedCommand, null);
    }

    private void LoadRecords_Click(object sender, RoutedEventArgs e)
    {
        ExecuteCommand(LoadRecordsCommand, null);
    }

    private static void ExecuteCommand(ICommand? command, object? parameter)
    {
        if (command?.CanExecute(parameter) == true)
        {
            command.Execute(parameter);
        }
    }
}
