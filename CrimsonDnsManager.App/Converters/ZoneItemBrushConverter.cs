using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace CrimsonDnsManager.App.Converters;

public sealed class ZoneItemBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var active = value is bool isActive && isActive;
        var target = parameter?.ToString()?.ToLowerInvariant() ?? "foreground";

        var color = target switch
        {
            "background" => active
                ? ColorHelper.FromArgb(26, 64, 200, 244)
                : ColorHelper.FromArgb(0, 0, 0, 0),
            "border" => active
                ? ColorHelper.FromArgb(61, 64, 200, 244)
                : ColorHelper.FromArgb(0, 0, 0, 0),
            _ => active
                ? ColorHelper.FromArgb(255, 255, 255, 255)
                : ColorHelper.FromArgb(255, 154, 184, 204)
        };

        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}
