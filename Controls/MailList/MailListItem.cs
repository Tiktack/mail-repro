using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using System.Globalization;

namespace MailPageRepro.Controls.MailList;

/// <summary>
/// Base class for all mail list display items.
/// </summary>
public abstract partial class MailListItemBase : ObservableObject;

/// <summary>
/// Represents a date group header in the mail list.
/// </summary>
public sealed partial class MailListDateHeader : MailListItemBase
{
    public DateOnly Date { get; init; }
    public string DisplayText { get; init; } = string.Empty;

    [ObservableProperty]
    public partial int EmailCount { get; set; }
}

/// <summary>
/// Base class for selectable email items.
/// </summary>
public abstract partial class MailListEmailItem : MailListItemBase
{
    public Guid Id { get; init; }
    public string ThreadId { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string PreviewText { get; init; } = string.Empty;
    public string SenderName { get; init; } = string.Empty;
    public string SenderEmail { get; init; } = string.Empty;
    public string ContactIconLookupValue { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public bool HasAttachments { get; init; }

    /// <summary>The owning account's label, shown only in multi-account (unified) lists; empty otherwise.</summary>
    public string AccountLabel { get; init; } = string.Empty;
    public bool HasAccountLabel => AccountLabel.Length > 0;

    [ObservableProperty]
    public partial ImageSource? ContactIcon { get; set; }

    [ObservableProperty]
    public partial bool IsRead { get; set; }

    [ObservableProperty]
    public partial bool IsStarred { get; set; }

    // Display helpers
    public string TimeDisplay => CreatedAt.ToLocalTime().ToString("HH:mm", CultureInfo.CurrentCulture);
    public string SenderDisplay => string.IsNullOrWhiteSpace(SenderName) ? SenderEmail : SenderName;
    public string SenderInitials => Utils.ContactDisplay.GetInitials(SenderDisplay);
}

/// <summary>
/// A single email (not part of a thread with multiple messages).
/// </summary>
public sealed partial class MailListSingleEmail : MailListEmailItem;

/// <summary>
/// A thread header representing multiple emails in a conversation.
/// </summary>
public sealed partial class MailListThreadItem : MailListEmailItem
{
    public string ThreadLookupKey { get; init; } = string.Empty;

    [ObservableProperty]
    public partial int ThreadCount { get; set; }

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    /// <summary>
    /// Child emails in the thread (not in the flat list until expanded).
    /// </summary>
    public List<MailListThreadChild> Children { get; } = [];
}

/// <summary>
/// A child email within an expanded thread.
/// </summary>
public sealed partial class MailListThreadChild : MailListEmailItem;
