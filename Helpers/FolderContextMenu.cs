using Microsoft.UI.Xaml.Media;
using MailPageRepro.ViewModels.Mail;
using Windows.Foundation;

namespace MailPageRepro.Helpers;

/// <summary>
/// Attached property that builds a folder's right-click <see cref="MenuFlyout"/> from a list of
/// <see cref="FolderMenuEntry"/> and assigns it as the target element's <c>ContextFlyout</c>, so the
/// folder menu is data-driven (bound via <c>x:Bind</c>) rather than hand-wired per action.
///
/// Items are constructed directly (no reflection) so it stays AOT-safe. Entries with children render as
/// nested <see cref="MenuFlyoutSubItem"/>s (e.g. the "Set color" color list); entries with a color render
/// a filled circle as their icon. A null or empty list clears the ContextFlyout, so grouping nodes /
/// non-folders get no menu.
/// </summary>
public static class FolderContextMenu
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.RegisterAttached(
            "ItemsSource",
            typeof(IReadOnlyList<FolderMenuEntry>),
            typeof(FolderContextMenu),
            new PropertyMetadata(null, OnItemsSourceChanged));

    public static IReadOnlyList<FolderMenuEntry>? GetItemsSource(DependencyObject obj)
        => (IReadOnlyList<FolderMenuEntry>?)obj.GetValue(ItemsSourceProperty);

    public static void SetItemsSource(DependencyObject obj, IReadOnlyList<FolderMenuEntry>? value)
        => obj.SetValue(ItemsSourceProperty, value);

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            return;

        if (e.NewValue is not IReadOnlyList<FolderMenuEntry> { Count: > 0 } entries)
        {
            element.ContextFlyout = null;
            return;
        }

        var flyout = new MenuFlyout();
        AddEntries(flyout.Items, entries);
        element.ContextFlyout = flyout;
    }

    private static void AddEntries(IList<MenuFlyoutItemBase> items, IReadOnlyList<FolderMenuEntry> entries)
    {
        foreach (var entry in entries)
        {
            if (entry.IsSeparator)
            {
                items.Add(new MenuFlyoutSeparator());
                continue;
            }

            if (entry.Children is { Count: > 0 } children)
            {
                var sub = new MenuFlyoutSubItem
                {
                    Text = entry.Text,
                    Icon = BuildIcon(entry),
                    IsEnabled = entry.IsEnabled,
                };
                AddEntries(sub.Items, children);
                items.Add(sub);
                continue;
            }

            items.Add(new MenuFlyoutItem
            {
                Text = entry.Text,
                Command = entry.Command,
                // A null command can't do anything, so never present it as enabled.
                IsEnabled = entry.IsEnabled && entry.Command is not null,
                Icon = BuildIcon(entry),
            });
        }
    }

    /// <summary>
    /// A filled circle of <see cref="FolderMenuEntry.ColorHex"/> when set (a color swatch); otherwise the
    /// entry's glyph, tinted with <see cref="FolderMenuEntry.GlyphColorHex"/> when set (theme default when
    /// not). Explicit foregrounds survive the menu item's pointer-over/pressed states.
    /// </summary>
    private static IconElement BuildIcon(FolderMenuEntry entry)
    {
        if (HexColor.TryParse(entry.ColorHex, out var swatch))
        {
            return new PathIcon
            {
                Data = new EllipseGeometry { Center = new Point(8, 8), RadiusX = 7, RadiusY = 7 },
                Foreground = new SolidColorBrush(swatch),
            };
        }

        var icon = new FontIcon { Glyph = entry.Glyph };
        if (HexColor.TryParse(entry.GlyphColorHex, out var tint))
            icon.Foreground = new SolidColorBrush(tint);
        return icon;
    }
}
