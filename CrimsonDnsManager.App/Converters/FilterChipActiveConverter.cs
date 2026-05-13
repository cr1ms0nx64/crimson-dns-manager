using Microsoft.UI.Xaml.Data;

namespace CrimsonDnsManager.App.Converters;

public sealed class FilterChipActiveConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var current = value?.ToString() ?? "Alle";
        var target = parameter?.ToString() ?? "Alle";
        return string.Equals(current, target, StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}
