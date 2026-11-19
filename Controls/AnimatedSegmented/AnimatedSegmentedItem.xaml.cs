using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;

namespace MailPageRepro.Controls;

/// <summary>
/// A pill that shows only its icon when idle and gracefully expands to icon + text
/// with an accent background when selected.
/// </summary>
public sealed partial class AnimatedSegmentedItem : UserControl
{
    private static readonly TimeSpan AnimationDuration = TimeSpan.FromMilliseconds(600);

    private bool _isSelected;
    private bool _isHovered;

    public event EventHandler? Click;

    public AnimatedSegmentedItem()
    {
        InitializeComponent();
        ActualThemeChanged += (_, _) => UpdateBackgroundState();
    }

    [GeneratedDependencyProperty]
    public partial string? Glyph { get; set; }

    [GeneratedDependencyProperty]
    public partial string? Text { get; set; }

    partial void OnGlyphChanged(string? newValue)
    {
        RestIcon.Glyph = newValue ?? string.Empty;
        SelectedIcon.Glyph = newValue ?? string.Empty;
    }

    partial void OnTextChanged(string? newValue)
    {
        Label.Text = newValue ?? string.Empty;
        ToolTipService.SetToolTip(PillButton, newValue);
        AutomationProperties.SetName(PillButton, newValue ?? string.Empty);

        // Keep the revealed width in sync if the text changes while selected.
        if (_isSelected)
        {
            ApplySelection(true, animate: false);
        }
    }

    /// <summary>
    /// Updates the selection state. Animations are only valid for pills that are
    /// already rendered — pass <paramref name="animate"/> false for initial state.
    /// </summary>
    public void SetSelected(bool selected, bool animate)
    {
        if (_isSelected == selected)
            return;

        _isSelected = selected;
        ApplySelection(selected, animate);
    }

    /// <summary>
    /// Resolves the state background in code instead of {ThemeResource}: live theme
    /// expressions for brushes unused elsewhere materialize an entire theme-dictionary
    /// branch (~64 MB) on the first minimize/restore. The template Border's
    /// BrushTransition animates the change on the composition thread.
    /// </summary>
    private void UpdateBackgroundState()
    {
        var key = _isSelected ? "AccentFillColorDefaultBrush"
            : _isHovered ? "SubtleFillColorTertiaryBrush"
            : "SubtleFillColorSecondaryBrush";
        PillButton.Background = (Brush)Application.Current.Resources[key];
    }

    private void ApplySelection(bool selected, bool animate)
    {
        UpdateBackgroundState();

        // DesiredSize already includes the label's margins (leading gap + trailing padding).
        Label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var textWidth = selected ? Label.DesiredSize.Width : 0;
        var selectedOpacity = selected ? 1d : 0d;

        if (!animate)
        {
            TextHost.MaxWidth = textWidth;
            SelectedIcon.Opacity = selectedOpacity;
            Label.Opacity = selectedOpacity;
            RestIcon.Opacity = 1 - selectedOpacity;
            return;
        }

        // Opacity cross-fades are independent animations (composition thread);
        // only the width reveal is layout-bound. Grow and shrink share the same
        // duration and easing, so two pills swapping selection move in lockstep.
        var storyboard = new Storyboard();
        AddAnimation(storyboard, TextHost, nameof(MaxWidth), textWidth, isDependent: true);
        AddAnimation(storyboard, SelectedIcon, nameof(Opacity), selectedOpacity);
        AddAnimation(storyboard, Label, nameof(Opacity), selectedOpacity);
        AddAnimation(storyboard, RestIcon, nameof(Opacity), 1 - selectedOpacity);
        storyboard.Begin();
    }

    private static void AddAnimation(Storyboard storyboard, DependencyObject target, string propertyPath, double to, bool isDependent = false)
    {
        var animation = new DoubleAnimation
        {
            To = to,
            Duration = new Duration(AnimationDuration),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            EnableDependentAnimation = isDependent
        };
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, propertyPath);
        storyboard.Children.Add(animation);
    }

    private void PillButton_Click(object sender, RoutedEventArgs e) => Click?.Invoke(this, EventArgs.Empty);

    private void PillButton_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isHovered = true;
        UpdateBackgroundState();
    }

    private void PillButton_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isHovered = false;
        UpdateBackgroundState();
    }
}
