using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace PCL.Modules.Base.Converters;

/// <summary>
/// Given a mod identifier string, produce a 36x36 circular Border with a colored background
/// and the first letter of the name centered. Pure visual placeholder; no image download.
/// </summary>
public sealed class ModIconConverter : IValueConverter
{
    private static readonly Color[] Palette =
    {
        Color.FromRgb(0x4F, 0x8F, 0xC7),
        Color.FromRgb(0x6F, 0x9F, 0x4F),
        Color.FromRgb(0xC7, 0x7F, 0x4F),
        Color.FromRgb(0x8F, 0x5F, 0x9F),
        Color.FromRgb(0x4F, 0xA7, 0xA7),
        Color.FromRgb(0xC7, 0x4F, 0x6F),
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var id = value as string ?? "";
        var letter = string.IsNullOrEmpty(id) ? "?" : char.ToUpperInvariant(id[0]).ToString();
        var color = Palette[Math.Abs(id.GetHashCode()) % Palette.Length];

        var border = new Border
        {
            Width = 32,
            Height = 32,
            CornerRadius = new CornerRadius(16),
            Background = new SolidColorBrush(color),
            Margin = new Thickness(4, 4, 4, 4),
            Child = new System.Windows.Controls.TextBlock
            {
                Text = letter,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            }
        };
        return border;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
