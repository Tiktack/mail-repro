using Microsoft.UI.Xaml.Media;

namespace MailPageRepro.Helpers;

/// <summary>
/// Attached property that paints a <see cref="TextBlock"/> or <see cref="IconElement"/>'s
/// <c>Foreground</c> from a provider-supplied hex color (e.g. Gmail label colors).
///
/// When the hex parses, the element gets a fixed <see cref="SolidColorBrush"/> — folder colors
/// are absolute and intentionally do not shift with the app theme. When the value is null/blank
/// or unparseable, the local Foreground is cleared so the element reverts to its inherited /
/// theme-default brush, which stays theme-reactive across light/dark switches (unlike a brush
/// captured from a <c>ThemeResource</c> in code). A direct x:Bind to this attached property
/// re-runs the callback on every change, including non-null → null, so recycled containers
/// reset correctly.
/// </summary>
public static class FolderColorBrush
{
    public static readonly DependencyProperty HexProperty =
        DependencyProperty.RegisterAttached(
            "Hex",
            typeof(string),
            typeof(FolderColorBrush),
            new PropertyMetadata(null, OnHexChanged));

    public static string? GetHex(DependencyObject obj) => (string?)obj.GetValue(HexProperty);

    public static void SetHex(DependencyObject obj, string? value) => obj.SetValue(HexProperty, value);

    private static void OnHexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (HexColor.TryParse(e.NewValue as string, out var color))
        {
            var brush = new SolidColorBrush(color);
            switch (d)
            {
                case TextBlock textBlock:
                    textBlock.Foreground = brush;
                    break;
                case IconElement iconElement:
                    iconElement.Foreground = brush;
                    break;
            }
        }
        else
        {
            switch (d)
            {
                case TextBlock textBlock:
                    textBlock.ClearValue(TextBlock.ForegroundProperty);
                    break;
                case IconElement iconElement:
                    iconElement.ClearValue(IconElement.ForegroundProperty);
                    break;
            }
        }
    }
}
