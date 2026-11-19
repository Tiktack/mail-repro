using Microsoft.UI.Xaml.Media.Animation;
using MailPageRepro.Views;

namespace MailPageRepro;

/// <summary>
/// Command-line switches (all optional):
///   --no-auto          skip the built-in measurement (test manually with Task Manager)
///   --cycles N         minimize/restore cycles for the built-in measurement (default 6)
///   --delay N          seconds after launch before an email is opened (default 4)
///   --flags a+b+c      strip switches that remove parts of the page (see README.md)
///   --page mail        host the mail page without the title-bar shell
/// </summary>
public partial class App : Application
{
    private Window? _window;
    private static double s_autoOpenDelay = 4;
    private static HashSet<string> s_flags = [];

    public static string FlagsText { get; private set; } = string.Empty;

#if REPRO_LAYOUT_FIXED
    public const string Layout = "Fixed";
#else
    public const string Layout = "Original";
#endif

    /// <summary>True when the strip switch <paramref name="flag"/> was passed in <c>--flags</c>.</summary>
    public static bool Has(string flag) => s_flags.Contains(flag);

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var argv = Environment.GetCommandLineArgs();
        string? Arg(string name) => Array.IndexOf(argv, name) is var i and >= 0 && i + 1 < argv.Length ? argv[i + 1] : null;

        if (double.TryParse(Arg("--delay"), System.Globalization.CultureInfo.InvariantCulture, out var delay))
            s_autoOpenDelay = delay;
        FlagsText = Arg("--flags") ?? string.Empty;
        s_flags = FlagsText.Split('+', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var cycles = int.TryParse(Arg("--cycles"), out var c) ? c : 6;

        _window = new MainWindow();
        var pageType = Arg("--page") == "mail" ? typeof(Views.Mail.MailView) : typeof(MainShellView);
        (_window.Content as Frame)?.Navigate(pageType, null, new SuppressNavigationTransitionInfo());
        _window.Activate();

        if (!argv.Contains("--no-auto"))
        {
            _ = AutoTest.RunAsync(_window, cycles, settleSeconds: s_autoOpenDelay + 8);
        }
    }

    /// <summary>
    /// Opens an email a few seconds after launch, like a user clicking a row, so the reader's first render
    /// happens after the window has been maximized.
    /// </summary>
    public static void ScheduleAutoOpen(FrameworkElement page, Action open)
    {
        var timer = page.DispatcherQueue.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(s_autoOpenDelay);
        timer.IsRepeating = false;
        timer.Tick += (_, _) => open();
        timer.Start();
    }
}
