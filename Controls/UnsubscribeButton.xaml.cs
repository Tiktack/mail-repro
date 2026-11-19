using CommunityToolkit.WinUI;

namespace MailPageRepro.Controls;

/// <summary>
/// Self-contained icon-only unsubscribe button: hand it an async action via <see cref="UnsubscribeAction"/> 
/// and it drives its own visuals — idle glyph →
/// progress ring while the action runs → transient check/error pop that settles back to the
/// glyph. No state is kept or exposed; every cycle starts from the same idle look.
/// </summary>
public sealed partial class UnsubscribeButton : UserControl
{
    /// <summary>Floor on the ring's screen time so near-instant actions don't flicker.</summary>
    private const int MinWorkingMs = 500;

    private bool _busy;

    public UnsubscribeButton()
    {
        InitializeComponent();
    }

    /// <summary>Returns true on success; false (or a throw) shows the error pop instead.</summary>
    [GeneratedDependencyProperty]
    public partial Func<Task<bool>>? UnsubscribeAction { get; set; }

    private async void HostButton_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || UnsubscribeAction is not { } action)
            return;

        _busy = true;
        ResultAnimation.Stop();
        WorkingRing.Visibility = Visibility.Visible;
        WorkingRing.IsActive = true;
        WorkingAnimation.Begin();

        bool success;
        try
        {
            var work = action();
            await Task.WhenAll(work, Task.Delay(MinWorkingMs));
            success = await work;
        }
        catch
        {
            success = false;
        }

        SuccessIcon.Visibility = success ? Visibility.Visible : Visibility.Collapsed;
        FailureIcon.Visibility = success ? Visibility.Collapsed : Visibility.Visible;

        WorkingAnimation.Stop();
        ResultAnimation.Begin();
    }

    private void ResultAnimation_Completed(object? sender, object e)
    {
        WorkingRing.IsActive = false;
        WorkingRing.Visibility = Visibility.Collapsed;
        _busy = false;
    }
}
