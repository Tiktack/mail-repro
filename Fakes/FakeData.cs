using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using MailPageRepro.Controls.MailList;
using MailPageRepro.ViewModels.Mail;
using Windows.Storage.Streams;

namespace MailPageRepro;

/// <summary>Static mail-page content shaped like a real inbox (date groups, threads, contact icons, unread badges).</summary>
internal static class FakeData
{
    private static readonly string[] Senders =
    [
        "Contoso", "Fabrikam support team", "Northwind", "Adventure Works", "Tailspin", "Alex Johnson",
        "Litware", "Proseware", "Woodgrove", "Coho Winery", "Fourth Coffee", "Lamna", "Wide World", "Margie's", "Blue Yonder",
    ];

    private static readonly string[] Subjects =
    [
        "Your subscription renews in 5 days", "New sign-in detected on your Fabrikam account",
        "Action required: please update your profile", "Important updates to our service",
        "Your order is ready for delivery", "-25% on selected items this week", "Your 10% coupon",
        "Your weekly summary", "[project] New comment on your task", "Thanks for your purchase!",
        "You have 12 new notifications", "Your monthly statement is ready", "Confirm your reservation",
        "An item on your list is on sale", "New releases for you",
    ];

    public static void FillNavigation(ObservableCollection<object> items)
    {
        items.Add(new UnifiedInboxNavigationItemViewModel { UnreadCount = 1 });

        var picture = Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", "icon4.png");
        var account = new AccountNavigationItemViewModel { Email = "someone@example.com", PictureUrl = picture, IsExpanded = true };
        var menu = new List<FolderMenuEntry>
        {
            new("Mark all as read", "", null),
            FolderMenuEntry.Separator,
            new("Rename", "", null),
        };
        account.Children.Add(new FolderNavigationItemViewModel { DisplayName = "Inbox", IconGlyph = "", UnreadCount = 1, ContextMenuItems = menu });
        account.Children.Add(new FolderNavigationItemViewModel { DisplayName = "Starred", IconGlyph = "", ContextMenuItems = menu });
        account.Children.Add(new FolderNavigationItemViewModel { DisplayName = "Sent", IconGlyph = "", ContextMenuItems = menu });
        account.Children.Add(new FolderNavigationItemViewModel { DisplayName = "Trash", IconGlyph = "", UnreadCount = 13, ContextMenuItems = menu });
        account.Children.Add(new FolderNavigationItemViewModel { DisplayName = "Spam", IconGlyph = "", ContextMenuItems = menu });
        account.Children.Add(new FolderNavigationItemViewModel { DisplayName = "Drafts", IconGlyph = "", ContextMenuItems = menu });
        var categories = new FolderNavigationItemViewModel { DisplayName = "Categories", IconGlyph = "", IsSelectable = false };
        categories.Children.Add(new FolderNavigationItemViewModel { DisplayName = "Updates", IconGlyph = "", Color = "#16a765" });
        var more = new FolderNavigationItemViewModel { DisplayName = "More", IconGlyph = "", IsSelectable = false };
        more.Children.Add(new FolderNavigationItemViewModel { DisplayName = "Archive", IconGlyph = "" });
        account.Children.Add(categories);
        account.Children.Add(more);
        items.Add(account);
    }

    public static void FillMailList(ObservableCollection<MailListItemBase> items)
    {
        var now = DateTime.Now;
        var groups = new[] { ("Today", 1), ("Yesterday", 5), ("Monday, November 16", 13), ("Sunday, November 15", 12) };
        var n = 0;
        foreach (var (title, count) in groups)
        {
            items.Add(new MailListDateHeader { DisplayText = title, EmailCount = count });
            for (var i = 0; i < count; i++, n++)
            {
                var sender = Senders[n % Senders.Length];
                var common = (Id: Guid.NewGuid(), Subject: Subjects[n % Subjects.Length], Sender: sender);
                MailListEmailItem item = n % 7 == 1
                    ? new MailListThreadItem
                    {
                        Id = common.Id, Subject = common.Subject, SenderName = sender, SenderEmail = $"no-reply@{sender.Replace(" ", "").ToLowerInvariant()}.com",
                        PreviewText = "Preview of the latest message in this conversation", CreatedAt = now.AddHours(-n), ThreadCount = 2,
                        ThreadLookupKey = common.Id.ToString(), ContactIconLookupValue = IconKey(n),
                    }
                    : new MailListSingleEmail
                    {
                        Id = common.Id, Subject = common.Subject, SenderName = sender, SenderEmail = $"no-reply@{sender.Replace(" ", "").ToLowerInvariant()}.com",
                        PreviewText = "Preview text of the message body, long enough to be trimmed by the list row",
                        CreatedAt = now.AddHours(-n), IsRead = n != 0, HasAttachments = n % 5 == 3, ContactIconLookupValue = IconKey(n),
                    };
                items.Add(item);
            }
        }
    }

    // Roughly two of three senders have a fetched icon; the rest fall back to initials.
    private static string IconKey(int n) => n % 3 == 2 ? string.Empty : (n % 6).ToString();

    /// <summary>Loads a contact icon the way a real client would: icon bytes, then BitmapImage.SetSourceAsync on a stream.</summary>
    public static async Task<ImageSource> LoadIconAsync(int index)
    {
        var bytes = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", $"icon{index % 6}.png"));
        using var stream = new InMemoryRandomAccessStream();
        using (var writer = new DataWriter(stream))
        {
            writer.WriteBytes(bytes);
            await writer.StoreAsync();
            writer.DetachStream();
        }
        stream.Seek(0);
        var bitmap = new BitmapImage();
        await bitmap.SetSourceAsync(stream);
        return bitmap;
    }
}
