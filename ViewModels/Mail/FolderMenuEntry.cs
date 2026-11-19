using System.Windows.Input;

namespace MailPageRepro.ViewModels.Mail;

/// <summary>
/// One item in a folder's right-click menu. Built by <c>FolderActionService.BuildMenu</c> and rendered
/// by the <c>FolderContextMenu.ItemsSource</c> attached property. Inapplicable / not-yet-implemented
/// actions are disabled stubs (<see cref="IsEnabled"/> false, null <see cref="Command"/>).
///
/// An entry with <see cref="Children"/> renders as a nested submenu (e.g. "Set color"). An entry with
/// <see cref="ColorHex"/> renders a filled color circle as its icon (a color swatch). An entry with
/// <see cref="GlyphColorHex"/> keeps its <see cref="Glyph"/> but tints it that color (null = theme
/// default) — used so the "Set color" header shows the folder's current color without changing its icon.
/// </summary>
public sealed record FolderMenuEntry(
    string Text,
    string Glyph,
    ICommand? Command,
    bool IsEnabled = true,
    bool IsSeparator = false,
    string? ColorHex = null,
    string? GlyphColorHex = null,
    IReadOnlyList<FolderMenuEntry>? Children = null)
{
    /// <summary>A divider between menu groups.</summary>
    public static FolderMenuEntry Separator { get; } =
        new(string.Empty, string.Empty, Command: null, IsEnabled: false, IsSeparator: true);
}
