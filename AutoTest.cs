using System.Diagnostics;
using System.Text;
using Microsoft.UI.Windowing;

namespace MailPageRepro;

/// <summary>
/// Built-in measurement so the repro needs no external tools: maximizes the window, lets the page open an email,
/// then minimizes and restores the window several times, sampling the process's memory after each restore, and
/// finally shows the results in a dialog (also written to LocalState\results.txt and the debug output).
/// </summary>
internal static class AutoTest
{
    private sealed record Sample(string Step, double CommitMB, double PrivateWorkingSetMB, double GpuMB);

    public static async Task RunAsync(Window window, int cycles, double settleSeconds)
    {
        if (window.AppWindow.Presenter is not OverlappedPresenter presenter)
            return;

        presenter.Maximize();
        // The page opens an email after --delay seconds; give the reader time to load and render at full size.
        await Task.Delay(TimeSpan.FromSeconds(settleSeconds));

        var samples = new List<Sample> { Take("start") };
        for (var i = 1; i <= cycles; i++)
        {
            presenter.Minimize();
            await Task.Delay(2000);
            presenter.Restore();
            await Task.Delay(3000);
            samples.Add(Take($"restore {i}"));
        }

        var report = Format(samples);
        Debug.WriteLine(report);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "results.txt"), report);
        }
        catch
        {
            // Best effort only; the dialog below is the primary output.
        }

        var dialog = new ContentDialog
        {
            XamlRoot = window.Content.XamlRoot,
            Title = "Restore memory test",
            CloseButtonText = "Close",
            Content = new ScrollViewer
            {
                Content = new TextBlock
                {
                    Text = report,
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                    IsTextSelectionEnabled = true,
                },
            },
        };
        await dialog.ShowAsync();
    }

    private static Sample Take(string step)
    {
        using var process = Process.GetCurrentProcess();
        return new Sample(step, process.PrivateMemorySize64 / 1048576.0, ReadPrivateWorkingSet(process), ReadGpuCommitted(process.Id));
    }

    // Task Manager's "Memory" column.
    private static double ReadPrivateWorkingSet(Process process)
    {
        try
        {
            using var counter = new PerformanceCounter("Process V2", "Working Set - Private", $"{process.ProcessName}:{process.Id}", readOnly: true);
            return counter.NextValue() / 1048576.0;
        }
        catch
        {
            return double.NaN;
        }
    }

    // GPU memory allocated by this process (on integrated/UMA GPUs it is also charged to process commit).
    private static double ReadGpuCommitted(int pid)
    {
        try
        {
            var category = new PerformanceCounterCategory("GPU Process Memory");
            double total = 0;
            foreach (var instance in category.GetInstanceNames().Where(n => n.StartsWith($"pid_{pid}_", StringComparison.Ordinal)))
            {
                using var counter = new PerformanceCounter("GPU Process Memory", "Total Committed", instance, readOnly: true);
                total += counter.NextValue();
            }
            return total / 1048576.0;
        }
        catch
        {
            return double.NaN;
        }
    }

    private static string Format(List<Sample> samples)
    {
        var start = samples[0];
        var peak = samples.Skip(1).MaxBy(s => s.CommitMB)!;
        var delta = peak.CommitMB - start.CommitMB;
        var sb = new StringBuilder();
        sb.AppendLine(delta > 30
            ? $"REPRODUCED: commit grew {delta:F0} MB after restoring (start {start.CommitMB:F0} MB, peak {peak.CommitMB:F0} MB at {peak.Step})."
            : $"Not reproduced: commit changed {delta:+0;-0} MB over {samples.Count - 1} restores.");
        sb.AppendLine($"Layout: {App.Layout}   Flags: {(App.FlagsText.Length > 0 ? App.FlagsText : "(none)")}");
        sb.AppendLine();
        sb.AppendLine($"{"Step",-11}{"Commit MB",11}{"Private WS MB",15}{"GPU MB",9}");
        foreach (var s in samples)
        {
            sb.AppendLine($"{s.Step,-11}{s.CommitMB,11:F1}{s.PrivateWorkingSetMB,15:F1}{s.GpuMB,9:F1}");
        }
        sb.AppendLine();
        sb.AppendLine("Commit = Process.PrivateMemorySize64. Private WS = Task Manager's \"Memory\" column.");
        sb.AppendLine("GPU = \"GPU Process Memory\\Total Committed\" for this process.");
        return sb.ToString();
    }
}
