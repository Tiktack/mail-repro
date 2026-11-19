using CommunityToolkit.WinUI;

namespace MailPageRepro.Controls;

/// <summary>
/// Horizontal row of <see cref="AnimatedSegmentedItem"/> pills with single selection.
/// The selected pill expands to show its text while the previous one shrinks back to its icon.
/// </summary>
public sealed partial class AnimatedSegmented : UserControl
{
    private bool _suppressAnimations;

    public AnimatedSegmented()
    {
        InitializeComponent();
    }

    [GeneratedDependencyProperty]
    public partial IReadOnlyList<ISegmentedItem>? ItemsSource { get; set; }

    partial void OnItemsSourceChanged(IReadOnlyList<ISegmentedItem>? newValue)
    {
        foreach (var pill in PillsPanel.Children.OfType<AnimatedSegmentedItem>())
        {
            pill.Click -= Pill_Click;
        }
        PillsPanel.Children.Clear();

        foreach (var item in newValue ?? [])
        {
            var pill = new AnimatedSegmentedItem
            {
                Glyph = item.IconGlyph,
                Text = item.DisplayName,
                Tag = item
            };
            pill.Click += Pill_Click;
            PillsPanel.Children.Add(pill);
        }

        // Fresh pills render their selection state directly: storyboards started
        // before the first render pass tick unreliably, and a rebuild should not
        // replay animations anyway. Selection changes within the same UI tick
        // (e.g. the initial SelectedItem binding) stay non-animated too.
        SyncSelection(animate: false);
        _suppressAnimations = true;
        DispatcherQueue.TryEnqueue(() => _suppressAnimations = false);
    }

    [GeneratedDependencyProperty]
    public partial ISegmentedItem? SelectedItem { get; set; }

    partial void OnSelectedItemChanged(ISegmentedItem? newValue) => SyncSelection(animate: !_suppressAnimations);

    private void Pill_Click(object? sender, EventArgs e)
    {
        if (sender is AnimatedSegmentedItem pill)
        {
            SelectedItem = pill.Tag as ISegmentedItem;
        }
    }

    private void SyncSelection(bool animate)
    {
        foreach (var pill in PillsPanel.Children.OfType<AnimatedSegmentedItem>())
        {
            pill.SetSelected(Equals(pill.Tag, SelectedItem), animate);
        }
    }
}
