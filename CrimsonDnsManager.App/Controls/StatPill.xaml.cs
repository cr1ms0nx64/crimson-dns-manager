using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace CrimsonDnsManager.App.Controls;

public sealed partial class StatPill : UserControl
{
    public StatPill()
    {
        InitializeComponent();
        ApplyDotVisibility();
        Loaded += (_, _) => ApplyDotVisibility();
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(StatPill), new PropertyMetadata(string.Empty, OnTextChanged));

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(StatPill), new PropertyMetadata(string.Empty, OnTextChanged));

    public static readonly DependencyProperty ShowDotProperty = DependencyProperty.Register(
        nameof(ShowDot), typeof(bool), typeof(StatPill), new PropertyMetadata(false, OnShowDotChanged));

    public static readonly DependencyProperty DotColorProperty = DependencyProperty.Register(
        nameof(DotColor), typeof(Color), typeof(StatPill), new PropertyMetadata(ColorHelper.FromArgb(255, 52, 212, 138), OnDotColorChanged));

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public bool ShowDot
    {
        get => (bool)GetValue(ShowDotProperty);
        set => SetValue(ShowDotProperty, value);
    }

    public Color DotColor
    {
        get => (Color)GetValue(DotColorProperty);
        set => SetValue(DotColorProperty, value);
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatPill pill)
        {
            pill.Bindings.Update();
        }
    }

    private static void OnShowDotChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatPill pill)
        {
            pill.ApplyDotVisibility();
        }
    }

    private static void OnDotColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatPill pill)
        {
            pill.ApplyDotColor();
        }
    }

    private void ApplyDotVisibility()
    {
        if (DotContainer is null)
        {
            return;
        }

        DotContainer.Visibility = ShowDot ? Visibility.Visible : Visibility.Collapsed;
        ApplyDotColor();
    }

    private void ApplyDotColor()
    {
        if (StatusDot is null || DotGlow is null)
        {
            return;
        }

        var brush = new SolidColorBrush(DotColor);
        StatusDot.Fill = brush;
        DotGlow.Fill = new SolidColorBrush(DotColor);
    }
}
