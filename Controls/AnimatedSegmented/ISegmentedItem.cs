namespace MailPageRepro.Controls;

/// <summary>
/// An item displayable by <see cref="AnimatedSegmented"/>.
/// </summary>
public interface ISegmentedItem
{
    string DisplayName { get; }
    string IconGlyph { get; }
}
