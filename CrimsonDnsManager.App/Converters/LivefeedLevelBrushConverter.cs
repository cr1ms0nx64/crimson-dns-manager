using CrimsonDnsManager.App.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace CrimsonDnsManager.App.Converters;

public sealed class LivefeedLevelBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var color = value is LivefeedLevel level
            ? level switch
            {
                LivefeedLevel.Success => ColorHelper.FromArgb(255, 52, 212, 138),
                LivefeedLevel.Warning => ColorHelper.FromArgb(255, 244, 192, 64),
                LivefeedLevel.Error => ColorHelper.FromArgb(255, 244, 80, 96),
                LivefeedLevel.Action => ColorHelper.FromArgb(255, 64, 200, 244),
                _ => ColorHelper.FromArgb(255, 90, 122, 150)
            }
            : ColorHelper.FromArgb(255, 90, 122, 150);

        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}
