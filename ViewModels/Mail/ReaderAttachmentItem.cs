using MailPageRepro.Services.MailRetrieval.Entities;
using System.Windows.Input;

namespace MailPageRepro.ViewModels.Mail;

/// <summary>
/// Attachment card model for the reader's attachment strip. Carries the owning ViewModel's
/// save command so the DataTemplate can bind it with x:Bind (templates can't reach the page VM).
/// </summary>
public sealed class ReaderAttachmentItem
{
    public required EmailAttachmentInfo Info { get; init; }
    public required ICommand SaveCommand { get; init; }

    public string Name => Info.Name;
    public string SizeDisplay => Info.SizeDisplay;

    /// <summary>Segoe Fluent Icons glyph for the attachment's media kind.</summary>
    public string IconGlyph
    {
        get
        {
            var type = Info.ContentType;
            if (type.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return "\uE91B"; // Photo
            if (type.StartsWith("video/", StringComparison.OrdinalIgnoreCase)) return "\uE714"; // Video
            if (type.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)) return "\uE90B"; // MusicInfo

            return type.ToLowerInvariant() switch
            {
                "application/pdf" => "\uEA90", // PDF
                "message/rfc822" => "\uE715", // Mail
                _ => "\uE8A5" // Document
            };
        }
    }
}
