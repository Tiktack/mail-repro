using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI.Text;

namespace MailPageRepro.Helpers;

public static class BindingExtensions
{
    public static Visibility ToVisibility(bool value)
    {
        return value ? Visibility.Visible : Visibility.Collapsed;
    }

    public static Visibility ToInverseVisibility(bool value)
    {
        return value ? Visibility.Collapsed : Visibility.Visible;
    }

    public static Visibility CountToVisibility(int count)
    {
        return count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public static bool Not(bool value)
    {
        return !value;
    }

    /// <summary>Unread rows are emphasized: SemiBold when not read, Normal when read.</summary>
    public static FontWeight ToUnreadFontWeight(bool isRead)
    {
        return isRead ? Microsoft.UI.Text.FontWeights.Normal : Microsoft.UI.Text.FontWeights.SemiBold;
    }

    /// <summary>Builds an ImageSource from an http(s) URL or a local file path; null when empty or invalid.</summary>
    public static ImageSource? ToImageSource(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        try
        {
            var uri = path.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? new Uri(path)
                : new Uri($"file:///{path.Replace('\\', '/')}");

            return new BitmapImage(uri);
        }
        catch
        {
            return null;
        }
    }
}
