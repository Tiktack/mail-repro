// Minimal entity/service stand-ins the page's XAML and small ViewModel types depend on.

namespace MailPageRepro.Entities.Database
{
    public sealed class Contact
    {
        public string Name { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
    }
}

namespace MailPageRepro.Services.MailRetrieval.Entities
{
    public sealed class EmailAttachmentInfo
    {
        public string Name { get; init; } = string.Empty;
        public string SizeDisplay { get; init; } = string.Empty;
        public string ContentType { get; init; } = string.Empty;
    }

    public enum MailReadFilter
    {
        All,
        Unread,
        Read,
    }

    public sealed class EmailHeader
    {
        public string Subject { get; init; } = string.Empty;
        public string SenderName { get; init; } = string.Empty;
        public string SenderEmail { get; init; } = string.Empty;
    }
}

namespace MailPageRepro
{
    /// <summary>Localized-string lookup from Resources.resw (falls back to the key).</summary>
    public static class Strings
    {
        private static readonly Microsoft.Windows.ApplicationModel.Resources.ResourceLoader Loader = new();

        public static string Get(string key)
        {
            try { return Loader.GetString(key) is { Length: > 0 } s ? s : key; }
            catch { return key; }
        }

        public static string Format(string key, params object[] args) => string.Format(Get(key), args);
    }
}
