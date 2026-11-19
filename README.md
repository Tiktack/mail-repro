# WinUI 3: ~70 MB of commit/GPU memory allocated on the first restore and never released

A WinUI 3 window showing a typical mail-client page permanently gains about **70 MB of committed memory** (and about
**40–50 MB of GPU memory**) the **first** time it is restored from minimized or resized. The memory is never given back.
Task Manager's *Memory* column then rises by ~16 MB on each of the next three restores, so it looks like a leak on every
restore, then stops.

Nothing in the app reacts to minimize or restore: no app code runs, no XAML is re-laid out and the managed heap doesn't
change. It's native allocations made by the in-process compositor/graphics stack. Small, ordinary changes to the visual
tree make it disappear completely (see [What makes it go away](#what-makes-it-go-away)).

## Reproduce (one step)

Requirements: Windows 11, Developer Mode on, .NET 10 SDK (or Visual Studio with the WinUI workload).

**Command line:**

```powershell
dotnet run
```

**Visual Studio:** open `MailPageRepro.slnx`, select profile **1. Repro**, then **Debug > Start Without Debugging**
(Ctrl+F5).

Then don't touch anything for about 45 seconds. The app maximizes itself, opens an email (as a user would), minimizes
and restores itself 6 times, and shows a dialog with the measurements:

```
REPRODUCED: commit grew 74 MB after restoring (start 88 MB, peak 162 MB at restore 1).
Layout: Original   Flags: (none)

Step         Commit MB  Private WS MB   GPU MB
start             88.1           75.7     73.5
restore 1        162.4           89.9    125.6
restore 2        156.1           99.9    125.6
restore 3        155.2          115.4    125.7
restore 4        155.1          130.7    125.7
restore 5        154.7          130.5    125.7
restore 6        154.5          131.1    125.7
```

The same report is written to `%LOCALAPPDATA%\Packages\<package family>\LocalState\results.txt`.

> **Run without a debugger or XAML diagnostics.** Visual Studio's in-app XAML toolbar / Live Visual Tree and WinApp's
> XAML DevTools inject their own visuals into the app, and that alone triggers the jump before the measurement starts
> (the "start" row is then already ~160 MB). The project sets `WinAppRunDevTools=off` so `dotnet run` is clean. In
> Visual Studio use *Start Without Debugging*.

### Manual check (optional)

Run profile **5. Manual** (or `dotnet run -p:WinAppRunArgs=--no-auto`), maximize the window, wait for the email to
open, and watch Task Manager > Details with the *Commit size*, *Memory (active private working set)* and *GPU
dedicated/shared memory* columns while you minimize and restore the window a few times.

## Environment where this was measured

| | |
|---|---|
| OS | Windows 11 Home, build 26340, ARM64 |
| Device | Snapdragon X (Qualcomm Adreno, driver 31.0.148.0). An integrated/UMA GPU, so GPU allocations are also charged to process commit |
| Display | 150% scale, window maximized at 2304 × 1464 |
| WinUI | `Microsoft.WindowsAppSDK.WinUI` 2.6.5, self-contained, packaged (MSIX) |
| .NET | 10 (also reproduced on .NET 11) |

On a machine with a discrete GPU, the GPU part may land in dedicated video memory instead of process commit. Watch the
**GPU MB** column (and Task Manager's GPU memory columns) as well as commit.

## What happens, in detail

1. The window is maximized and an email is opened; the page renders normally (≈88 MB commit, ≈73 MB GPU).
2. On minimize, GPU memory drops (surfaces are released).
3. On the **first restore**, the process allocates seven new 8 MB `MEM_PRIVATE` regions (8 MB = a 2048 × 1024 BGRA
   page) plus ~8 MB of heap growth, about +70 MB in total. GPU "Total Committed" rises to ~125 MB. None of it is ever
   released, including through later minimize/restore cycles, resizes, or idle time.
4. Private working set (Task Manager's *Memory*) climbs ~16 MB per restore for three more restores as those pages get
   touched, then stays flat. So the growth is bounded, not an unbounded leak.
5. A normal → maximized **resize** triggers the same one-time allocation when it hasn't happened yet.

What was ruled out while investigating (removing it doesn't change the result): WebView2, Mica, WinUIEx, the XAML
optional-change flags (`XamlOptionalChanges`), `NavigationView` pane-display mode, list `ItemContainerTransitions`,
background work and timers, and the managed heap (a GC dump before and after is flat at ~4 MB). There is no
`SizeChanged` on minimize/restore, so nothing is re-laid out.

## What makes it go away

The memory jump behaves like a **threshold over the whole page**, not a bug in one control. Each row below is a fresh
process using the built-in test (2 runs each):

| Variant | How to run | Result |
|---|---|---|
| Full page | profile 1 / `dotnet run` | **+74 MB** |
| Title bar without the `AnimatedIcon` settings button and the Toolkit `Segmented` | profile 2, `--flags noanimicon+nosegmentedui` | flat |
| Title bar header removed entirely | `--flags noheader` | flat |
| Full title bar, no nav-pane `InfoBadge`s | `--flags nobadges` | **+74 MB** |
| Full title bar, no list avatars | `--flags noavatars` | **+74 MB** |
| Full title bar, no rounded corners on the reader cards | `--flags noreaderradius` | **+74 MB** |
| Title bar keeps only the `AnimatedIcon` button | profile 3, `--flags nostatus+nosegmentedui` | **+74 MB** |
| …and no nav-pane `InfoBadge`s | profile 4, `--flags nostatus+nosegmentedui+nobadges` | flat |
| Rounded cards drawn by childless `Border`s ("Fixed" layout) | `dotnet build -p:ReproLayout=Fixed`, then run | flat |

With the title bar cut down to only the animated settings icon, removing the list avatars, or the reader's rounded corners,
was also enough to make it flat (measured earlier with external minimize/restore).

Everything involved is **vector/shape content**: the Lottie `AnimatedIcon` (composition shapes), the `Segmented` pill,
`InfoBadge` pills, `Ellipse` avatars with `ImageBrush`, and rounded `CornerRadius` backgrounds/clips. Our working
hypothesis is that after the window is hidden and shown, the compositor re-rasterizes this content and, past some budget,
spills into new texture pages that it never frees. We couldn't confirm that from outside.

### The "Fixed" layout (workaround)

The two layouts differ only in how the three rounded cards (mail list, reader header, reader body) are drawn:

- **Original:** `CornerRadius="8"` on the `Grid`/`Border` that **contains** the card's content, which also rounds and
  clips the children.
- **Fixed:** a **childless** `Border` with `CornerRadius="8"` draws the card background behind the content, the content
  sits in a sibling `Grid Margin="1"`, and nothing clips to a rounded shape.

Both look the same. Compare `Layouts/Original/*.xaml` with `Layouts/Fixed/*.xaml`.

## Project layout

| Path | What it is |
|---|---|
| `Views/MainShellView.xaml` | Shell: `TitleBar` with icon, title, `Segmented`, a sync-status control and an `AnimatedIcon` settings button, plus a `Frame` |
| `Layouts/<Original\|Fixed>/MailView.xaml` | `NavigationView` folder pane (items with `InfoBadge`s), mail-list card, reader host |
| `Controls/MailList/MailListControl.xaml` | `ListView` with date headers, threads, and avatar rows (`Ellipse` + `ImageBrush`, or initials) |
| `Layouts/<Original\|Fixed>/MailReaderView.xaml` | Reader: header card (subject, sender, `CommandBar`) and body card |
| `Fakes/` | Placeholder data and minimal ViewModels the XAML binds to |
| `AutoTest.cs` | The built-in maximize → open email → minimize/restore × N → report test |

All data is fake (placeholder company names and colored-square icons). There's no network access, no accounts and no
WebView2.

### Command-line switches

`--no-auto` skips the built-in test. `--cycles N` sets the number of restores (default 6). `--delay N` sets the seconds
before the email opens (default 4). `--page mail` shows the mail page without the title-bar shell (doesn't reproduce).
`--flags a+b+c` removes parts of the page:

| Flag | Removes |
|---|---|
| `noheader`, `nostatus`, `nosegmentedui`, `noanimicon`, `nosettings`, `noicon`, `notitletext`, `notitlebar` | title-bar pieces |
| `nonavpane`, `nonavitems`, `nobadges`, `nopanecustom` | nav-pane pieces |
| `nolist`, `noavatars`, `noicons` | mail-list pieces |
| `noreaderhead`, `noreaderbody`, `noactions`, `noreaderradius` | reader pieces |

From the command line: `dotnet run -p:WinAppRunArgs="--flags noheader"`.
