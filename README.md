# WinUI 3: the compositor keeps ~65 MB of GPU memory after the first frame that draws 199+ primitives

The first time a single frame draws **199 or more solid-color primitives**, the WinUI 3 compositor
(Microsoft.UI.Composition, in process) allocates about **65 MB** of GPU memory: seven 8 MB `MEM_PRIVATE` blocks plus
some heap growth. **198** primitives don't trigger it. The memory is never released: not after more minimize/restore
cycles, not after idling, and not after removing every visual from the tree.

The repro uses plain colored rectangles: raw `SpriteVisual`s with one `CompositionColorBrush`, or plain XAML `Border`s.
There are no controls, images, text, or animations.

## Why apps see it as "memory grows on the first restore"

At startup, app content usually appears in pieces (shell first, then a list, then a reader pane). Each piece only
redraws its own dirty region, so no single frame reaches 199 primitives. Minimizing drops the window's surfaces, and the
**restore is the first frame that redraws the whole window**. If the window holds 199 or more primitives in total, that
frame triggers the allocation. In Task Manager it then looks like a leak on restore: *Commit size* and GPU memory jump
once, and *Memory* (private working set) climbs over the next few restores as the pages are touched.

## Run

Windows 11 and the .NET 10 SDK. Unpackaged and self-contained, so no Developer Mode or MSIX registration is needed:

```powershell
dotnet run
```

The window maximizes, adds 150 rectangles, adds 100 more below them 2 s later, then minimizes and restores itself three
times. After about 20 s it shows the results, and also writes them to `results.txt` next to the exe:

```
250 SpriteVisuals: 150 at startup + 100 added 2 s later

            commit MB   GPU MB   client px
start            46.0     34.6   2304x1430
restore 1       118.6     86.5   2304x1430     <- +73 MB commit, +52 MB GPU, never released
restore 2       117.3     86.6   2304x1430
restore 3       116.7     86.7   2304x1430
```

| Command | Result |
|---|---|
| `dotnet run` | **jumps on restore 1** (250 rectangles, 150 + 100) |
| `dotnet run -- --count 190` | flat |
| `dotnet run -- --count 198 --later 0` | flat (198 at once) |
| `dotnet run -- --count 199 --later 0` | **jumps at startup** (199 at once) |
| `dotnet run -- --xaml` | same as the first row, with XAML `Border` elements instead of SpriteVisuals |
| `dotnet run -p:WinUIVersion=2.7.0` | same; also checked with 1.8.260204000 |

Other switches: `--cycles C`, `--size WxH` (client size in pixels instead of maximized), `--exit` (close when done).

## Environment

| | |
|---|---|
| OS | Windows 11 Home 10.0.26340, ARM64 |
| GPU | Qualcomm Adreno (Snapdragon X), integrated/UMA, so GPU allocations are also charged to process commit |
| Display | 2304 × 1464 at 150% |
| WinAppSDK / WinUI | `Microsoft.WindowsAppSDK.WinUI` 2.6.5 (also 1.8.260204000 and 2.7.0) |

On a discrete GPU the memory may show up as dedicated GPU memory instead of process commit, so watch the **GPU MB**
column as well as commit.

## What we found

- **Exact count threshold:** 198 primitives in one frame → flat, 199 → +65 MB. This is the same with SpriteVisuals and
  with XAML elements.
- **Only primitives that are drawn count.** Elements outside the window are culled and don't count. A frame that
  redraws only a dirty region counts only the primitives in that region.
- **Size and opacity of the primitives don't matter.** 400 sprites of 1 × 1 DIP trigger it, and so do translucent
  sprites.
- **The allocation is the same size whatever the window size** (seven 8 MB blocks). Whether it triggers does depend
  on window size, but not in a simple way. Maximized windows and most sizes trigger it. On this machine, a band of
  client sizes around 820–1050 × 525–690 px doesn't trigger it even with 8,000 primitives, while smaller windows
  (for example 656 × 388) do. We can't explain that band from outside.
- **Never released.** Removing every visual, then more minimize/restore cycles: the memory stays.
- **Not a recent regression:** the same on WinUI 1.8.260204000, 2.6.5 and 2.7.0.

### What a "primitive" is, for XAML elements

The cost of common elements against the 199 budget, measured by finding how many of each trigger the jump in a
maximized window:

| Element | Triggers at | ≈ primitives each |
|---|---|---|
| `Border` with a solid `Background` (square corners) | 199 | 1 |
| `Ellipse` with a solid `Fill` | 199 | 1 |
| `Border` with `CornerRadius="8"` and a `Background` | 48 | ~4 |
| `Border` with `CornerRadius="8"` and only a `BorderBrush` | 42 | ~5 |
| `Border` with `CornerRadius="8"`, `Background` **and a child** (rounded clip) | 27 | ~7 |
| `InfoBadge` | 38 | ~5 |
| `TextBlock`, `Button`, `AnimatedIcon` (17 px) | no jump with 420 | ~0 |

Rounded corners, and rounded clipping of children in particular, use up the budget fast. That's how we found this:
a real mail-client page (a title bar, a folder pane with badges, a list with avatars, a reader pane with rounded cards)
gained ~74 MB on the first restore. Small, unrelated-looking changes (removing the badges, the avatars, or the rounded
clips) each made the problem go away. A variant of that page that drew its rounded cards with childless `Border`s
stayed flat. Adding *N* extra 1 × 1 sprites to that variant: +64 stayed flat, and **+65 reproduced the full +74 MB
restore jump**. So a full redraw of that page drew 134 primitives, and 134 + 65 = 199. Each of those changes only
brought the page below the threshold.


## Workarounds for apps

There's no real fix on the app side. You can only stay below 199 solid-color primitives per full frame:

- Avoid `CornerRadius` on containers whose children get clipped. Draw rounded cards with a childless `Border` behind
  the content instead.
- Use fewer rounded/solid decorations (pills, badges, dividers, colored dots) in views that are on screen together.
