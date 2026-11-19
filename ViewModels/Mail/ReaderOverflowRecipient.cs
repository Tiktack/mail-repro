using MailPageRepro.Entities.Database;
using MailPageRepro.Utils;

namespace MailPageRepro.ViewModels.Mail;

/// <summary>
/// One row in the "+N more" recipients flyout: initials avatar, name, email, and the
/// recipient kind (To/Cc/Bcc) since the overflow can span kinds.
/// </summary>
public sealed class ReaderOverflowRecipient
{
    public required Contact Contact { get; init; }
    public required string KindLabel { get; init; }

    public string DisplayName => string.IsNullOrWhiteSpace(Contact.Name) ? Contact.Email : Contact.Name;
    public string Initials => ContactDisplay.GetInitials(DisplayName);
    public string Email => Contact.Email;

    /// <summary>The email line is shown only when it adds information over the name.</summary>
    public bool ShowEmail => Email.Length > 0
        && !string.Equals(Email, DisplayName, StringComparison.OrdinalIgnoreCase);
}
