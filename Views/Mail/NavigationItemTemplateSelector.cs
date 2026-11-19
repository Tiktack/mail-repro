using MailPageRepro.ViewModels.Mail;

namespace MailPageRepro.Views.Mail;

public partial class NavigationItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? AccountTemplate { get; set; }
    public DataTemplate? FolderTemplate { get; set; }
    public DataTemplate? UnifiedInboxTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is AccountNavigationItemViewModel)
            return AccountTemplate;
        else if (item is FolderNavigationItemViewModel)
            return FolderTemplate;
        else if (item is UnifiedInboxNavigationItemViewModel)
            return UnifiedInboxTemplate;

        return base.SelectTemplateCore(item);
    }
}
