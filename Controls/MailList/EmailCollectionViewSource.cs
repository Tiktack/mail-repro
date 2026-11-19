using System.Collections.ObjectModel;

namespace MailPageRepro.Controls.MailList;

/// <summary>Minimal mail-list source: a flat, pre-grouped row list.</summary>
public sealed class EmailCollectionViewSource
{
    public ObservableCollection<MailListItemBase> Items { get; } = [];

    public void SetThreadExpanded(string threadLookupKey, bool isExpanded)
    {
        if (Items.OfType<MailListThreadItem>().FirstOrDefault(t => t.ThreadLookupKey == threadLookupKey) is { } thread)
        {
            thread.IsExpanded = isExpanded;
        }
    }
}
