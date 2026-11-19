// Fake ViewModels exposing exactly the members the mail-page XAML binds to, filled with static data.
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MailPageRepro.Controls;
using MailPageRepro.Controls.MailList;
using MailPageRepro.Entities.Database;
using MailPageRepro.Services.MailRetrieval.Entities;
using MailPageRepro.Views.Mail;

namespace MailPageRepro.ViewModels.Mail
{
    public sealed record MailSegment(string DisplayName, string IconGlyph) : ISegmentedItem;

    public sealed class UnifiedInboxNavigationItemViewModel
    {
        public string DisplayName { get; init; } = "All Inboxes";
        public string ToolTipText => DisplayName;
        public string IconGlyph { get; init; } = "";
        public bool IsSelectable => true;
        public int UnreadCount { get; init; }
    }

    public sealed partial class AccountNavigationItemViewModel : ObservableObject
    {
        public required string Email { get; init; }
        public string ToolTipText => Email;
        public string? PictureUrl { get; init; }
        public bool IsSelectable => false;
        public ObservableCollection<object> Children { get; } = [];

        [ObservableProperty]
        public partial bool IsExpanded { get; set; }
    }

    public sealed partial class FolderNavigationItemViewModel : ObservableObject
    {
        public required string DisplayName { get; init; }
        public string ToolTipText => DisplayName;
        public required string IconGlyph { get; init; }
        public string? Color { get; init; }
        public bool IsSelectable { get; init; } = true;
        public int UnreadCount { get; init; }
        public IReadOnlyList<FolderMenuEntry>? ContextMenuItems { get; init; }
        public ObservableCollection<object> Children { get; } = [];

        [ObservableProperty]
        public partial bool IsExpanded { get; set; }
    }

    public sealed partial class MailViewModel : ObservableObject
    {
        public ObservableCollection<object> NavigationItems { get; } = [];
        public IReadOnlyList<ISegmentedItem> Segments { get; } = new List<ISegmentedItem> { new MailSegment("All Inboxes", "") };
        public EmailCollectionViewSource EmailSource { get; } = new();

        public IRelayCommand<MailListEmailItem> ToggleReadCommand { get; } = new RelayCommand<MailListEmailItem>(_ => { });
        public IRelayCommand<MailListEmailItem> ToggleStarCommand { get; } = new RelayCommand<MailListEmailItem>(_ => { });
        public IRelayCommand<MailListEmailItem> DeleteCommand { get; } = new RelayCommand<MailListEmailItem>(_ => { });

        [ObservableProperty] public partial object? SelectedNavigationItem { get; set; }
        [ObservableProperty] public partial ISegmentedItem? SelectedSegment { get; set; }
        [ObservableProperty] public partial bool ShowSegments { get; set; } = true;
        [ObservableProperty] public partial bool ShowEmptyState { get; set; }
        [ObservableProperty] public partial bool ShowEmailList { get; set; } = true;
        [ObservableProperty] public partial MailReadFilter ReadFilter { get; set; }
        [ObservableProperty] public partial bool IsReadFilterActive { get; set; }
        [ObservableProperty] public partial bool IsInfoBarOpen { get; set; }
        [ObservableProperty] public partial string InfoBarMessage { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CurrentTabType))]
        public partial MailListEmailItem? SelectedMailItem { get; set; }

        public MailViewTabs CurrentTabType => SelectedMailItem is null ? MailViewTabs.None : MailViewTabs.MailReader;

        public MailViewModel()
        {
            SelectedSegment = Segments[0];
            FakeData.FillNavigation(NavigationItems);
            SelectedNavigationItem = NavigationItems[0];
            FakeData.FillMailList(EmailSource.Items);
        }

        public EmailHeader? GetSelectedEmail(MailListEmailItem? item) => item is null
            ? null
            : new EmailHeader { Subject = item.Subject, SenderName = item.SenderName, SenderEmail = item.SenderEmail };
    }

    public sealed partial class MailReaderViewModel : ObservableObject
    {
        public IRelayCommand DeleteCommand { get; } = new RelayCommand(() => { });
        public IRelayCommand ToggleReaderThemeCommand { get; } = new RelayCommand(() => { });
        public IRelayCommand ToggleStarCommand { get; } = new RelayCommand(() => { });
        public IRelayCommand MarkUnreadCommand { get; } = new RelayCommand(() => { });
        public IRelayCommand RetryLoadBodyCommand { get; } = new RelayCommand(() => { });

        [ObservableProperty] public partial string Subject { get; set; } = string.Empty;
        [ObservableProperty] public partial string FormattedDate { get; set; } = "09:30";
        [ObservableProperty] public partial string FullDate { get; set; } = "Monday, January 5, 2026 09:30";
        [ObservableProperty] public partial string SenderDisplayName { get; set; } = string.Empty;
        [ObservableProperty] public partial string SenderEmail { get; set; } = string.Empty;
        [ObservableProperty] public partial bool ShowSenderEmail { get; set; } = true;
        [ObservableProperty] public partial ImageSource? SenderIcon { get; set; }
        [ObservableProperty] public partial string SenderInitials { get; set; } = string.Empty;
        [ObservableProperty] public partial bool IsUnsubscribeAvailable { get; set; }
        [ObservableProperty] public partial Func<Task<bool>>? UnsubscribeAction { get; set; }
        [ObservableProperty] public partial bool HasRecipients { get; set; } = true;
        [ObservableProperty] public partial IReadOnlyList<ReaderRecipientChip> RecipientChips { get; set; } = [];
        [ObservableProperty] public partial IReadOnlyList<ReaderOverflowRecipient> AllRecipients { get; set; } = [];
        [ObservableProperty] public partial IReadOnlyList<ReaderAttachmentItem>? Attachments { get; set; }
        [ObservableProperty] public partial bool IsLoading { get; set; }
        [ObservableProperty] public partial bool HasBodyError { get; set; }
        [ObservableProperty] public partial string BodyErrorMessage { get; set; } = string.Empty;
        [ObservableProperty] public partial bool CanRetryBody { get; set; }
        [ObservableProperty] public partial bool IsBodyVisible { get; set; } = true;
        [ObservableProperty] public partial bool IsReaderDark { get; set; }
        [ObservableProperty] public partial bool IsAppThemeDark { get; set; }

        [ObservableProperty] public partial string FlyoutContactName { get; set; } = string.Empty;
        [ObservableProperty] public partial string FlyoutContactEmail { get; set; } = string.Empty;
        [ObservableProperty] public partial bool FlyoutHasEmail { get; set; }
        [ObservableProperty] public partial ImageSource? FlyoutContactIcon { get; set; }
        [ObservableProperty] public partial string FlyoutContactInitials { get; set; } = string.Empty;

        [ObservableProperty]
        public partial EmailHeader? Email { get; set; }

        partial void OnEmailChanged(EmailHeader? value)
        {
            if (value is null)
                return;
            Subject = value.Subject;
            SenderDisplayName = value.SenderName;
            SenderEmail = value.SenderEmail;
            SenderInitials = Utils.ContactDisplay.GetInitials(value.SenderName);
            var me = new Contact { Name = "Sam Taylor", Email = "me@example.com" };
            RecipientChips = new List<ReaderRecipientChip> { new() { LeadingText = "To:", Contact = me } };
            AllRecipients = new List<ReaderOverflowRecipient> { new() { Contact = me, KindLabel = "To" } };
            _ = LoadSenderIconAsync();
        }

        private async Task LoadSenderIconAsync() => SenderIcon = await FakeData.LoadIconAsync(0);

        public Task PrepareContactFlyoutAsync(Contact contact)
        {
            FlyoutContactName = contact.Name;
            FlyoutContactEmail = contact.Email;
            FlyoutHasEmail = true;
            FlyoutContactInitials = Utils.ContactDisplay.GetInitials(contact.Name);
            return Task.CompletedTask;
        }

        public void Cleanup() { }
    }
}

namespace MailPageRepro.ViewModels
{
    public sealed partial class AccountSyncStatusViewModel : ObservableObject
    {
        public required string AccountEmail { get; init; }
        public string? ProfilePictureUrl { get; init; }
        public IRelayCommand StartSyncForAccountCommand { get; } = new RelayCommand(() => { });
        [ObservableProperty] public partial bool IsSyncing { get; set; }
        [ObservableProperty] public partial bool IsIndeterminate { get; set; }
        [ObservableProperty] public partial bool IsPercentageShown { get; set; }
        [ObservableProperty] public partial double SyncProgress { get; set; }
        public string DetailedSyncInfo => "Synced";
    }

    public sealed partial class SyncStatusViewModel : ObservableObject
    {
        public ObservableCollection<AccountSyncStatusViewModel> Accounts { get; } = [new AccountSyncStatusViewModel { AccountEmail = "me@example.com" }];
        public bool HasAccounts => Accounts.Count > 0;
        [ObservableProperty] public partial bool IsSyncing { get; set; }
    }
}

namespace MailPageRepro.Services
{
    /// <summary>Settings stand-in; MailView binds Settings.State.*.</summary>
    public sealed class SettingsService
    {
        public SettingsState State { get; } = new();
    }

    public sealed partial class SettingsState : ObservableObject
    {
        [ObservableProperty] public partial bool IsNavigationPaneOpen { get; set; } = true;
        public double MailListWidth { get; set; } = 400;
        public bool UseContactIcons { get; set; } = true;
    }
}
