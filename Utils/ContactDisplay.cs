namespace MailPageRepro.Utils;

public static class ContactDisplay
{
    /// <summary>One- or two-letter initials for avatar fallbacks ("Joseph Brown" → "JB").</summary>
    public static string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "?";
        if (parts.Length == 1) return parts[0][..1].ToUpperInvariant();
        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}
