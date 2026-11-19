using MailPageRepro.Entities.Database;

namespace MailPageRepro.ViewModels.Mail;

/// <summary>
/// One recipient in the reader header's inline "To: A; B; Cc: C;" line (clicking opens the
/// contact flyout). <see cref="LeadingText"/> carries the kind label on the first chip of
/// each kind; the trailing semicolon is part of the button text so the line reads as prose
/// while every name stays an individually clickable element. The view shows as many chips
/// as fit the pane width and summarizes the hidden rest as "+N more".
/// </summary>
public sealed class ReaderRecipientChip
{
    public required string LeadingText { get; init; }
    public required Contact Contact { get; init; }

    public string DisplayName => string.IsNullOrWhiteSpace(Contact.Name) ? Contact.Email : Contact.Name;

    public string ButtonText => $"{DisplayName};";
    public string TooltipText => Contact.Email;
    public bool HasLeadingText => LeadingText.Length > 0;
}
