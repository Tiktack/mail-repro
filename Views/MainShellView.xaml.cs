using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using MailPageRepro.ViewModels;
using MailPageRepro.Views.Mail;

namespace MailPageRepro.Views;

// Shell flow: the segmented control's initial selection navigates AppShell to MailView.
public sealed partial class MainShellView : Page
{
    private readonly SyncStatusViewModel SyncStatusViewModel = new();

    public MainShellView()
    {
        InitializeComponent();
        AppShell.Navigated += AppShell_Navigated;

        // Strip variants can remove the segmented control that normally triggers this navigation.
        Loaded += (_, _) =>
        {
            if (AppShell.Content is null)
                AppShell.Navigate(typeof(MailView), null, new DrillInNavigationTransitionInfo());
        };

        if (App.Has("notitlebar")) MainTitleBar.Visibility = Visibility.Collapsed;
        if (App.Has("noheader")) MainTitleBar.RightHeader = null;       // segmented + status + settings
        if (App.Has("nostatus") && MainTitleBar.RightHeader is StackPanel h1) h1.Children.RemoveAt(1);
        if (App.Has("nosettings") && MainTitleBar.RightHeader is StackPanel h2) h2.Children.RemoveAt(h2.Children.Count - 1);
        if (App.Has("nosegmentedui")) ViewModeSegmented.Opacity = 0;    // keep it: its selection drives navigation
        if (App.Has("noanimicon")) SettingsButton.Content = new FontIcon { Glyph = "", FontSize = 16 };
        if (App.Has("noicon")) MainTitleBar.IconSource = null;
        if (App.Has("notitletext")) { MainTitleBar.Title = null; MainTitleBar.Subtitle = null; }
    }

    private void AppShell_Navigated(object sender, NavigationEventArgs e)
    {
        MainTitleBar.IsPaneToggleButtonVisible = e.Content is IHasNavigationView;
    }

    private void MainTitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        if (AppShell.Content is IHasNavigationView view && view.GetNavigationView() is { } navView)
        {
            navView.IsPaneOpen = !navView.IsPaneOpen;
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e) { }

    private void ViewModeSegmented_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Can fire during InitializeComponent, before AppShell exists.
        if (sender is Segmented { SelectedItem: SegmentedItem { Tag: "mail" } })
        {
            AppShell?.Navigate(typeof(MailView), null, new DrillInNavigationTransitionInfo());
        }
    }

    private void MainTitleBar_BackRequested(TitleBar sender, object args) => AppShell.GoBack();

    private void AppShell_Navigating(object sender, NavigatingCancelEventArgs e) { }
}

public interface IHasNavigationView
{
    NavigationView? GetNavigationView();
}
