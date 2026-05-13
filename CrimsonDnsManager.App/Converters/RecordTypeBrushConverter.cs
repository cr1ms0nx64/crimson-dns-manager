using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace CrimsonDnsManager.App.Converters;

public sealed class RecordTypeBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var color = (value as string) switch
        {
            "A" => ColorHelper.FromArgb(255, 64, 200, 244),
            "AAAA" => ColorHelper.FromArgb(255, 120, 220, 255),
            "CNAME" => ColorHelper.FromArgb(255, 244, 192, 64),
            "MX" => ColorHelper.FromArgb(255, 167, 139, 250),
            "TXT" => ColorHelper.FromArgb(255, 52, 212, 138),
            "SRV" => ColorHelper.FromArgb(255, 251, 146, 60),
            _ => ColorHelper.FromArgb(255, 154, 184, 204)
        };

        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}
