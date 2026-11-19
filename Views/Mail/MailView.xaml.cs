using MailPageRepro.Services;
using MailPageRepro.Services.MailRetrieval.Entities;
using MailPageRepro.ViewModels.Mail;
using System.ComponentModel;

namespace MailPageRepro.Views.Mail;

public sealed partial class MailView : Page, IHasNavigationView
{
    private readonly MailViewModel ViewModel = new();
    public SettingsService Settings { get; } = new();

    /// <summary>
    /// Mail-list column width, dragged via the PropertySizer. The column is updated live;
    /// the value is persisted when the resize gesture ends, not per pointer move.
    /// </summary>
    public double MailListWidth
    {
        get => MailListColumn.Width.Value;
        set
        {
            if (MailListColumn.Width.Value != value)
            {
                MailListColumn.Width = new GridLength(value);
            }
        }
    }

    public MailView()
    {
        InitializeComponent();
        if (App.Has("nonavpane")) AccountsNavigationView.IsPaneVisible = false;
        if (App.Has("nonavitems"))
        {
            var s = new Style(typeof(NavigationViewItem));
            s.Setters.Add(new Setter(VisibilityProperty, Visibility.Collapsed));
            AccountsNavigationView.Resources[typeof(NavigationViewItem)] = s;
        }
        if (App.Has("nobadges"))
        {
            var s = new Style(typeof(InfoBadge));
            s.Setters.Add(new Setter(Control.TemplateProperty, Microsoft.UI.Xaml.Markup.XamlReader.Load(
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='InfoBadge'><Grid/></ControlTemplate>")));
            AccountsNavigationView.Resources[typeof(InfoBadge)] = s;
        }
        if (App.Has("nopanecustom")) AccountsNavigationView.PaneCustomContent = null;
        if (App.Has("nolist")) MailList.Opacity = 0;
        MailList.LoadMoreRequested += MailList_LoadMoreRequested;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        MailListColumn.Width = new GridLength(Math.Clamp(Settings.State.MailListWidth, MailListSizer.Minimum, MailListSizer.Maximum));
        // PropertySizer has no drag-completed API of its own; it resizes via standard
        // manipulation events (pointer drag) and arrow keys, so these two cover both.
        MailListSizer.ManipulationCompleted += MailListSizer_ResizeEnded;
        MailListSizer.KeyUp += MailListSizer_ResizeEnded;
    }

    /// <summary>
    /// Creates the reader on first use and keeps its <see cref="MailReaderView.Email"/> in sync
    /// with the list selection. Kept out of XAML so the reader's large template is not parsed
    /// during startup.
    /// </summary>
    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(MailViewModel.SelectedMailItem) or nameof(MailViewModel.CurrentTabType)))
        {
            return;
        }

        if (ViewModel.CurrentTabType != MailViewTabs.MailReader)
        {
            // Mirror the original binding on deselection so the reader cancels any in-flight
            // load and reselecting the same message triggers a fresh load.
            if (ReaderHost.Content is MailReaderView existingReader)
            {
                existingReader.Email = null;
            }
            return;
        }

        if (ReaderHost.Content is not MailReaderView reader)
        {
            reader = new MailReaderView();
            ReaderHost.Content = reader;
        }

        reader.Email = ViewModel.GetSelectedEmail(ViewModel.SelectedMailItem);
    }

    private void MailListSizer_ResizeEnded(object sender, RoutedEventArgs e)
        => Settings.State.MailListWidth = MailListColumn.Width.Value;

    public NavigationView? GetNavigationView() => AccountsNavigationView;

    private void AccountsNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        // For a child selected before its container exists, WinUI defers this event
        // and moves the indicator, but can leave the realized container unselected.
        // Use the event's container: DataTemplate x:Bind does not require DataContext.
        if (args.SelectedItemContainer is { IsSelected: false } container)
        {
            container.IsSelected = true;
        }
    }

    private async void MailList_LoadMoreRequested(object? sender, EventArgs e)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// x:Bind helper for checking the read-filter radio items.
    /// </summary>
    public bool IsReadFilter(MailReadFilter value, string name) => value.ToString() == name;

    private void ReadFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioMenuFlyoutItem { Tag: string tag })
        {
            ViewModel.ReadFilter = Enum.Parse<MailReadFilter>(tag);
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        App.ScheduleAutoOpen(this, () => ViewModel.SelectedMailItem = ViewModel.EmailSource.Items.OfType<MailPageRepro.Controls.MailList.MailListSingleEmail>().First());
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        MailList.LoadMoreRequested -= MailList_LoadMoreRequested;
        MailList.Dispose();
        MailListSizer.ManipulationCompleted -= MailListSizer_ResizeEnded;
        MailListSizer.KeyUp -= MailListSizer_ResizeEnded;
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }
}

public enum MailViewTabs
{
    MailReader,
    MailComposer,
    None
}
