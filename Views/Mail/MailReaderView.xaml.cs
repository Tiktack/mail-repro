using CommunityToolkit.WinUI;
using MailPageRepro.Services.MailRetrieval.Entities;
using MailPageRepro.ViewModels.Mail;

namespace MailPageRepro.Views.Mail;

// Reader code-behind. The original reader hosts the email body in a WebView2; it is omitted because the issue reproduces without it.
public sealed partial class MailReaderView : UserControl
{
    public MailReaderViewModel ViewModel { get; } = new();

    public MailReaderView() => InitializeComponent();

    [GeneratedDependencyProperty]
    public partial EmailHeader? Email { get; set; }

    partial void OnEmailChanged(EmailHeader? newValue)
    {
        ViewModel.Email = newValue;
    }

    public static Visibility AttachmentsVisibility(IReadOnlyList<ReaderAttachmentItem>? attachments)
    {
        return attachments is { Count: > 0 } ? Visibility.Visible : Visibility.Collapsed;
    }

    private const char SunGlyph = (char)0xE706;
    private const char MoonGlyph = (char)0xE708;

    public static string ThemeGlyph(bool isReaderDark) => (isReaderDark ? MoonGlyph : SunGlyph).ToString();

    private const double NarrowHeaderThreshold = 620;

    private bool _isHeaderNarrow;

    private void MailReaderView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var isNarrow = e.NewSize.Width < NarrowHeaderThreshold;
        if (isNarrow == _isHeaderNarrow)
            return;

        _isHeaderNarrow = isNarrow;
        VisualStateManager.GoToState(this, isNarrow ? "NarrowHeader" : "WideHeader", useTransitions: false);

        if (isNarrow)
        {
            ActionsBar.PrimaryCommands.Remove(ReplyAllButton);
            ActionsBar.PrimaryCommands.Remove(ForwardButton);
            ActionsBar.PrimaryCommands.Remove(ActionsSeparator);
            ActionsBar.PrimaryCommands.Remove(DarkModeToggle);
            ActionsBar.SecondaryCommands.Insert(0, ReplyAllButton);
            ActionsBar.SecondaryCommands.Insert(1, ForwardButton);
            ActionsBar.SecondaryCommands.Insert(2, DarkModeToggle);
        }
        else
        {
            ActionsBar.SecondaryCommands.Remove(ReplyAllButton);
            ActionsBar.SecondaryCommands.Remove(ForwardButton);
            ActionsBar.SecondaryCommands.Remove(DarkModeToggle);
            ActionsBar.PrimaryCommands.Insert(1, ReplyAllButton);
            ActionsBar.PrimaryCommands.Insert(2, ForwardButton);
            ActionsBar.PrimaryCommands.Insert(3, ActionsSeparator);
            ActionsBar.PrimaryCommands.Add(DarkModeToggle);
        }
    }

    private Controls.SingleLineFitPanel? _recipientsPanel;

    private void RecipientsPanel_Loaded(object sender, RoutedEventArgs e)
    {
        _recipientsPanel = (Controls.SingleLineFitPanel)sender;
        _recipientsPanel.HiddenCountChanged += RecipientsPanel_HiddenCountChanged;
        UpdateRecipientsOverflow();
    }

    private void RecipientsPanel_Unloaded(object sender, RoutedEventArgs e)
    {
        ((Controls.SingleLineFitPanel)sender).HiddenCountChanged -= RecipientsPanel_HiddenCountChanged;
        if (ReferenceEquals(_recipientsPanel, sender))
        {
            _recipientsPanel = null;
        }
    }

    private void RecipientsPanel_HiddenCountChanged(object? sender, EventArgs e) => UpdateRecipientsOverflow();

    private void UpdateRecipientsOverflow()
    {
        var hidden = _recipientsPanel?.HiddenCount ?? 0;
        RecipientsMoreButton.Visibility = hidden > 0 ? Visibility.Visible : Visibility.Collapsed;
        RecipientsMoreText.Text = Strings.Format("MailReader_RecipientsMore", hidden);
    }

    private async void RecipientChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: ReaderRecipientChip chip } element)
            return;

        var prepare = ViewModel.PrepareContactFlyoutAsync(chip.Contact);
        if (Resources["ContactFlyout"] is Flyout flyout)
        {
            flyout.ShowAt(element);
        }

        await prepare;
    }

    private void RecipientsOverflow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && Resources["RecipientsOverflowFlyout"] is Flyout flyout)
        {
            flyout.ShowAt(element);
        }
    }

    private void MailReaderView_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.IsAppThemeDark = ActualTheme == ElementTheme.Dark;

        // Strip switches (indices match the Original layout: [0] header card, [1] body card).
        var root = (Grid)Content;
        if (App.Has("noreaderhead")) root.Children[0].Visibility = Visibility.Collapsed;
        if (App.Has("noreaderbody")) root.Children[1].Visibility = Visibility.Collapsed;
        if (App.Has("noactions")) ActionsBar.Visibility = Visibility.Collapsed;
        if (App.Has("noreaderradius"))
        {
            foreach (var child in root.Children)
            {
                if (child is Grid g) g.CornerRadius = default;
                if (child is Border b) b.CornerRadius = default;
            }
            WebviewGrid.CornerRadius = default;
        }
    }

    private void MailReaderView_Unloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.Cleanup();
    }
}
