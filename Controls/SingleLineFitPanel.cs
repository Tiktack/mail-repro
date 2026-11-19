using Windows.Foundation;

namespace MailPageRepro.Controls;

/// <summary>
/// Lays out children left-to-right on a single line at their natural widths, hiding the
/// suffix that does not fit (no wrapping). The first child always stays visible, trimmed to
/// the available width when even it alone overflows. <see cref="HiddenCount"/> reports how
/// many children are hidden so a companion "+N more" element can summarize the rest; it
/// changes during layout, so consumers react via <see cref="HiddenCountChanged"/>.
/// </summary>
public sealed partial class SingleLineFitPanel : Panel
{
    // Absorbs sub-pixel layout-rounding noise so the last fitting child is not hidden spuriously.
    private const double FitTolerance = 0.5;

    public double Spacing { get; set; }

    public int HiddenCount { get; private set; }

    public event EventHandler? HiddenCountChanged;

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = 0, height = 0;
        var children = Children;
        var visibleCount = 0;

        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            if (visibleCount < i)
                continue; // a predecessor already overflowed; this child hides (still measured above)

            var desired = child.DesiredSize;
            var spacing = i > 0 ? Spacing : 0;
            if (width + spacing + desired.Width > availableSize.Width + FitTolerance)
            {
                if (i > 0)
                    continue;

                // Sole-survivor case: the first child trims instead of hiding.
                child.Measure(new Size(Math.Max(0, availableSize.Width), availableSize.Height));
                desired = child.DesiredSize;
            }

            width += spacing + desired.Width;
            height = Math.Max(height, desired.Height);
            visibleCount++;
        }

        SetHiddenCount(children.Count - visibleCount);
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var children = Children;
        var visibleCount = children.Count - HiddenCount;
        double x = 0;

        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            if (i >= visibleCount)
            {
                child.Arrange(new Rect(0, 0, 0, 0));
                continue;
            }

            if (i > 0)
                x += Spacing;

            var width = Math.Min(child.DesiredSize.Width, Math.Max(0, finalSize.Width - x));
            child.Arrange(new Rect(x, 0, width, finalSize.Height));
            x += width;
        }

        return finalSize;
    }

    private void SetHiddenCount(int hiddenCount)
    {
        if (HiddenCount == hiddenCount)
            return;

        HiddenCount = hiddenCount;
        HiddenCountChanged?.Invoke(this, EventArgs.Empty);
    }
}
