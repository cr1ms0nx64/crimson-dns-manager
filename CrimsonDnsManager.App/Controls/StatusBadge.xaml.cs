using System.Collections.Generic;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace CrimsonDnsManager.App.Controls;

public sealed partial class StatusBadge : UserControl
{
    private static readonly Dictionary<string, (Color text, Color border, Color bg)> VariantMap = new()
    {
        ["ok"] = (ColorHelper.FromArgb(255, 52, 212, 138), ColorHelper.FromArgb(71, 52, 212, 138), ColorHelper.FromArgb(20, 52, 212, 138)),
        ["warn"] = (ColorHelper.FromArgb(255, 244, 192, 64), ColorHelper.FromArgb(71, 244, 192, 64), ColorHelper.FromArgb(20, 244, 192, 64)),
        ["err"] = (ColorHelper.FromArgb(255, 244, 80, 96), ColorHelper.FromArgb(71, 244, 80, 96), ColorHelper.FromArgb(20, 244, 80, 96)),
        ["neu"] = (ColorHelper.FromArgb(255, 64, 200, 244), ColorHelper.FromArgb(71, 64, 200, 244), ColorHelper.FromArgb(20, 64, 200, 244)),
        ["gray"] = (ColorHelper.FromArgb(255, 90, 122, 150), ColorHelper.FromArgb(61, 90, 122, 150), ColorHelper.FromArgb(20, 90, 122, 150)),
    };

    public StatusBadge()
    {
        InitializeComponent();
        ApplyVariant(Variant);
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(StatusBadge), new PropertyMetadata(string.Empty, OnTextChanged));

    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
        nameof(Variant), typeof(string), typeof(StatusBadge), new PropertyMetadata("gray", OnVariantChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Variant
    {
        get => (string)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatusBadge badge)
        {
            badge.Bindings.Update();
        }
    }

    private static void OnVariantChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatusBadge badge)
        {
            badge.ApplyVariant(e.NewValue as string);
        }
    }

    private void ApplyVariant(string? variant)
    {
        if (BadgeBorder is null)
        {
            return;
        }

        var key = NormalizeVariant(variant);
        if (!VariantMap.TryGetValue(key, out var colors))
        {
            colors = VariantMap["gray"];
        }

        BadgeDot.Fill = new SolidColorBrush(colors.text);
        BadgeText.Foreground = new SolidColorBrush(colors.text);
        BadgeBorder.BorderBrush = new SolidColorBrush(colors.border);
        BadgeBorder.Background = new SolidColorBrush(colors.bg);
    }

    private static string NormalizeVariant(string? variant)
    {
        return variant?.Trim().ToLowerInvariant() switch
        {
            "new" => "neu",
            "error" => "err",
            "danger" => "err",
            null or "" => "gray",
            var value => value,
        };
    }
}
