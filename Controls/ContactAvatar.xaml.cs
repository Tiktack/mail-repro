using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Media;

namespace MailPageRepro.Controls;

/// <summary>
/// Round contact avatar: the contact icon (domain favicon) on a light surface when available,
/// an initials circle otherwise. Size with Width/Height; initials size with FontSize.
/// </summary>
public sealed partial class ContactAvatar : UserControl
{
    public ContactAvatar() => InitializeComponent();

    [GeneratedDependencyProperty]
    public partial ImageSource? Icon { get; set; }

    [GeneratedDependencyProperty]
    public partial string? Initials { get; set; }

    // The icon/initials switch is imperative because x:Bind function bindings are not
    // re-evaluated when their argument transitions to null — a recycled list row whose
    // Icon goes back to null would keep showing the empty icon surface instead of initials.
    partial void OnIconChanged(ImageSource? newValue)
    {
        var hasIcon = newValue is not null;
        IconSurface.Visibility = hasIcon ? Visibility.Visible : Visibility.Collapsed;
        IconImage.Visibility = hasIcon ? Visibility.Visible : Visibility.Collapsed;
        InitialsPanel.Visibility = hasIcon ? Visibility.Collapsed : Visibility.Visible;
    }
}
