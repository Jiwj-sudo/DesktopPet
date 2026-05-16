using System.Globalization;
using System.Windows.Data;
using DeskCat.Models;

namespace DeskCat.Converters;

public sealed class StateToAnimationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is PetState state && AnimationConfig.Defaults.TryGetValue(state, out var config)
            ? config.FileName
            : "idle.png";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
