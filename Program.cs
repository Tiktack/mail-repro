// Minimal repro: the WinUI 3 compositor (Microsoft.UI.Composition) permanently allocates ~65 MB of GPU memory
// (seven 8 MB blocks) the first time a single frame draws 199 or more solid-color primitives. Nothing is released
// afterwards, not even when every visual is removed.
//
// In a real app this usually first happens on the first restore from minimized. At startup the content shows up in
// pieces, and each piece only redraws its own dirty region. The restore is the first frame that redraws everything.
//
// The app draws plain colored rectangles (no images, no text, no controls) and measures that:
//   1. maximize, add (count - later) rectangles, then 2 s later add `later` more below them (a partial redraw)
//   2. minimize and restore `cycles` times, sampling memory after each restore
//   3. show the results in the window and write them to results.txt next to the exe
//
// Switches:
//   --count N    rectangles in total (default 250)
//   --later K    how many of them are added 2 s after startup (default 100; 0 = all at startup)
//   --xaml       use XAML Border elements instead of raw composition SpriteVisuals (same result)
//   --cycles C   minimize/restore cycles (default 3)
//   --size WxH   client size in px instead of maximized
//   --exit       close the app when done (for scripted runs)
//
//   (defaults)              -> commit and GPU memory jump ~65 MB on restore 1 and stay there
//   --count 190             -> flat
//   --count 198 --later 0   -> flat
//   --count 199 --later 0   -> the same jump, but at startup

using System.Diagnostics;
using System.Numerics;
using System.Text;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;

namespace MinimalRepro;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(_ =>
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
            new App();
        });
    }
}

public sealed class App : Application
{
    private static readonly string[] Args = Environment.GetCommandLineArgs();

    private static string? Arg(string name) => Array.IndexOf(Args, name) is var i and >= 0 && i + 1 < Args.Length ? Args[i + 1] : null;

    protected override async void OnLaunched(LaunchActivatedEventArgs e)
    {
        int count = int.Parse(Arg("--count") ?? "250");
        int later = Math.Min(int.Parse(Arg("--later") ?? "100"), count);
        int cycles = int.Parse(Arg("--cycles") ?? "3");
        bool xaml = Args.Contains("--xaml");
        int[]? size = Arg("--size")?.Split('x').Select(int.Parse).ToArray();

        var window = new Window { Title = "MinimalRepro" };
        var root = new Grid();
        var canvas = new Canvas();
        var report = new TextBlock { Margin = new Thickness(700, 20, 20, 20), FontFamily = new("Consolas"), IsTextSelectionEnabled = true };
        root.Children.Add(canvas);
        root.Children.Add(report);
        window.Content = root;

        var compositor = ElementCompositionPreview.GetElementVisual(root).Compositor;
        var sprites = compositor.CreateContainerVisual();
        ElementCompositionPreview.SetElementChildVisual(canvas, sprites);
        var spriteBrush = compositor.CreateColorBrush(Colors.SteelBlue);
        var xamlBrush = new SolidColorBrush(Colors.SteelBlue);

        // Adds n 30x12 rectangles in rows of 18, starting at the given row.
        void AddRectangles(int n, int firstRow)
        {
            for (var i = 0; i < n; i++)
            {
                float x = i % 18 * 36 + 10, y = (firstRow + i / 18) * 18 + 10;
                if (xaml)
                {
                    var border = new Border { Width = 30, Height = 12, Background = xamlBrush };
                    Canvas.SetLeft(border, x);
                    Canvas.SetTop(border, y);
                    canvas.Children.Add(border);
                }
                else
                {
                    var sprite = compositor.CreateSpriteVisual();
                    sprite.Brush = spriteBrush;
                    sprite.Size = new Vector2(30, 12);
                    sprite.Offset = new Vector3(x, y, 0);
                    sprites.Children.InsertAtTop(sprite);
                }
            }
        }

        window.Activate();
        var presenter = (OverlappedPresenter)window.AppWindow.Presenter;
        if (size is null) presenter.Maximize();
        else window.AppWindow.ResizeClient(new(size[0], size[1]));

        AddRectangles(count - later, 0);
        await Task.Delay(2000);
        AddRectangles(later, (count - later + 17) / 18 + 1); // one empty row below the first batch
        await Task.Delay(3000);

        string Sample(string step)
        {
            using var process = Process.GetCurrentProcess();
            return $"{step,-10} {process.PrivateMemorySize64 / 1048576.0,10:F1} {GpuCommittedMB(process.Id),8:F1}   " +
                   $"{window.AppWindow.ClientSize.Width}x{window.AppWindow.ClientSize.Height}";
        }

        var rows = new List<string> { Sample("start") };
        for (var i = 1; i <= cycles; i++)
        {
            presenter.Minimize();
            await Task.Delay(1500);
            if (size is null) presenter.Maximize(); // like clicking the taskbar button: back to maximized, no resize
            else presenter.Restore();
            await Task.Delay(2500);
            rows.Add(Sample($"restore {i}"));
        }

        var text = new StringBuilder()
            .AppendLine($"{count} {(xaml ? "XAML Borders" : "SpriteVisuals")}: {count - later} at startup + {later} added 2 s later")
            .AppendLine()
            .AppendLine($"{"",-10} {"commit MB",10} {"GPU MB",8}   client px")
            .AppendJoin(Environment.NewLine, rows)
            .ToString();
        report.Text = text;
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "results.txt"), text);
        if (Args.Contains("--exit")) Exit();
    }

    // GPU memory allocated by this process. On integrated (UMA) GPUs it is also charged to process commit.
    // NaN when the "GPU Process Memory" counters aren't available on the machine.
    private static double GpuCommittedMB(int pid)
    {
        try
        {
            var category = new PerformanceCounterCategory("GPU Process Memory");
            return category.GetInstanceNames()
                .Where(n => n.StartsWith($"pid_{pid}_", StringComparison.Ordinal))
                .Sum(n =>
                {
                    using var counter = new PerformanceCounter("GPU Process Memory", "Total Committed", n, readOnly: true);
                    return counter.NextValue();
                }) / 1048576.0;
        }
        catch
        {
            return double.NaN;
        }
    }
}
