using System.Globalization;
using Windows.UI;

namespace MailPageRepro.Helpers;

/// <summary>
/// Parses CSS-style hex color strings ("#RRGGBB" / "#AARRGGBB", with or without the leading '#') into a
/// <see cref="Color"/>. Shared by the UI surfaces that accept provider-supplied folder colors
/// (<see cref="FolderColorBrush"/>, <see cref="FolderContextMenu"/>) so the parsing lives in one place.
/// </summary>
public static class HexColor
{
    /// <summary>Parses the hex string; returns false (and a default color) when blank or malformed.</summary>
    public static bool TryParse(string? hex, out Color color)
    {
        color = default;

        if (string.IsNullOrWhiteSpace(hex))
            return false;

        var span = hex.AsSpan().Trim();
        if (span.Length > 0 && span[0] == '#')
            span = span[1..];

        byte a = 0xFF;
        int rgbStart;
        if (span.Length == 8)
        {
            if (!byte.TryParse(span[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out a))
                return false;
            rgbStart = 2;
        }
        else if (span.Length == 6)
        {
            rgbStart = 0;
        }
        else
        {
            return false;
        }

        if (!byte.TryParse(span.Slice(rgbStart, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r) ||
            !byte.TryParse(span.Slice(rgbStart + 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g) ||
            !byte.TryParse(span.Slice(rgbStart + 4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
        {
            return false;
        }

        color = Color.FromArgb(a, r, g, b);
        return true;
    }
}
