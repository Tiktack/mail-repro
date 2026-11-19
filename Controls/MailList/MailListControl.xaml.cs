using CommunityToolkit.WinUI;
using System.Windows.Input;

namespace MailPageRepro.Controls.MailList;

/// <summary>
/// Simple mail list control that renders an EmailCollectionViewSource using ListView.
/// All grouping, threading, and data management is handled by the data source.
/// </summary>
public sealed partial class MailListControl : UserControl, IDisposable
{
    private const int LoadMoreThreshold = 12;
    private bool _disposed;
    private MailListEmailItem? _contextMenuItem;

    public event EventHandler? LoadMoreRequested;

    public MailListControl()
    {
        InitializeComponent();
        if (App.Has("noavatars"))
        {
            var s = new Style(typeof(ContactAvatar));
            s.Setters.Add(new Setter(VisibilityProperty, Visibility.Collapsed));
            MailListView.Resources[typeof(ContactAvatar)] = s;
        }
        if (App.Has("noicons")) _noIcons = true;
    }

    #region Dependency Properties

    [GeneratedDependencyProperty]
    public partial EmailCollectionViewSource? Source { get; set; }

    partial void OnSourceChanged(EmailCollectionViewSource? newValue)
    {
        if (MailListView is null)
            return;

        MailListView.ItemsSource = newValue?.Items;
    }

    [GeneratedDependencyProperty]
    public partial MailListEmailItem? SelectedItem { get; set; }

    /// <summary>Command executed with the row item when the user toggles read state from the context menu.</summary>
    [GeneratedDependencyProperty]
    public partial ICommand? ToggleReadCommand { get; set; }

    /// <summary>Command executed with the row item when the user toggles the star from the context menu.</summary>
    [GeneratedDependencyProperty]
    public partial ICommand? ToggleStarCommand { get; set; }

    /// <summary>Command executed with the row item when the user deletes (moves to Trash) from the context menu.</summary>
    [GeneratedDependencyProperty]
    public partial ICommand? DeleteCommand { get; set; }

    partial void OnSelectedItemChanged(MailListEmailItem? newValue)
    {
        if (MailListView is null)
            return;

        // Sync ListView selection when property changes from outside
        if (MailListView.SelectedItem != newValue)
        {
            MailListView.SelectedItem = newValue;
        }
    }

    #endregion

    /// <summary>x:Bind helper: chevron glyph for thread expanders (right when collapsed, down when expanded, like TreeView).</summary>
    public static string ChevronGlyph(bool isExpanded) => isExpanded ? "\uE70D" : "\uE76C"; // ChevronDown : ChevronRight

    #region Event Handlers

    private void MailListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // If a date header was selected, clear selection (headers are not selectable)
        if (MailListView.SelectedItem is MailListDateHeader)
        {
            MailListView.SelectedItem = null;
            return;
        }

        // Update the dependency property (supports two-way binding)
        SelectedItem = MailListView.SelectedItem as MailListEmailItem;
    }

    private void MailListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        RequestMoreItemsIfNeeded(args);

        if (args.InRecycleQueue)
            return;

        if (args.ItemContainer is not ListViewItem container)
            return;

        // Apply non-selectable behavior to date header containers
        if (args.Item is MailListDateHeader)
        {
            container.IsHitTestVisible = false;
            container.IsTabStop = false;
        }
        else
        {
            container.IsHitTestVisible = true;
            container.IsTabStop = true;
        }

        if (args.Item is MailListEmailItem emailItem)
        {
            RequestContactIcon(emailItem);
        }
    }

    private void ExpandThread_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not MailListThreadItem threadItem || Source is null)
            return;

        Source.SetThreadExpanded(threadItem.ThreadLookupKey, !threadItem.IsExpanded);
    }

    private void MailItemContextFlyout_Opening(object? sender, object e)
    {
        if (sender is not MenuFlyout flyout)
            return;

        // The flyout is shared across row templates; resolve the row it opened on.
        _contextMenuItem = (flyout.Target as FrameworkElement)?.DataContext as MailListEmailItem;
        if (_contextMenuItem is null)
        {
            flyout.Hide();
            return;
        }

        if (flyout.Items.Count >= 2 && flyout.Items[0] is MenuFlyoutItem readItem && flyout.Items[1] is MenuFlyoutItem starItem)
        {
            readItem.Text = Strings.Get(_contextMenuItem.IsRead ? "MailList_MarkUnread" : "MailList_MarkRead");
            starItem.Text = Strings.Get(_contextMenuItem.IsStarred ? "MailList_RemoveStar" : "MailList_Star");
        }
    }

    private void ToggleReadFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        if (_contextMenuItem is MailListEmailItem item && ToggleReadCommand?.CanExecute(item) == true)
            ToggleReadCommand.Execute(item);
    }

    private void ToggleStarFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        if (_contextMenuItem is MailListEmailItem item && ToggleStarCommand?.CanExecute(item) == true)
            ToggleStarCommand.Execute(item);
    }

    private void DeleteFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        if (_contextMenuItem is MailListEmailItem item && DeleteCommand?.CanExecute(item) == true)
            DeleteCommand.Execute(item);
    }

    #endregion

    private void RequestMoreItemsIfNeeded(ContainerContentChangingEventArgs args)
    {
        if (args.InRecycleQueue || Source is null)
            return;

        var itemCount = Source.Items.Count;
        if (itemCount == 0 || args.ItemIndex < itemCount - LoadMoreThreshold)
            return;

        LoadMoreRequested?.Invoke(this, EventArgs.Empty);
    }

    // Contact icons load like a real client's: icon bytes, then a stream-sourced BitmapImage assigned to the row.
    private bool _noIcons;

    private async void RequestContactIcon(MailListEmailItem item)
    {
        if (_noIcons || item.ContactIcon is not null || !int.TryParse(item.ContactIconLookupValue, out var index))
            return;

        item.ContactIcon = await FakeData.LoadIconAsync(index);
    }

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        MailListView.ItemsSource = null;
    }

    #endregion
}

/// <summary>
/// Template selector for mail list items.
/// </summary>
public sealed partial class MailListTemplateSelector : DataTemplateSelector
{
    public DataTemplate? DateHeaderTemplate { get; set; }
    public DataTemplate? EmailTemplate { get; set; }
    public DataTemplate? ThreadHeaderTemplate { get; set; }
    public DataTemplate? ThreadChildTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
        => item is MailListItemBase row ? SelectRowTemplate(row) : null;

    private DataTemplate? SelectRowTemplate(MailListItemBase row) => row switch
    {
        MailListDateHeader => DateHeaderTemplate,
        MailListSingleEmail => EmailTemplate,
        MailListThreadItem => ThreadHeaderTemplate,
        MailListThreadChild => ThreadChildTemplate,
        _ => null,
    };

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}
