using CommunityToolkit.WinUI;
using MailPageRepro.ViewModels;

namespace MailPageRepro.Controls;

public sealed partial class StatusControl : UserControl
{
    public StatusControl()
    {
        InitializeComponent();
    }

    [GeneratedDependencyProperty]
    public partial SyncStatusViewModel? ViewModel { get; set; }

    private void AddAccountButton_Click(object sender, RoutedEventArgs e) => StatusFlyout.Hide();
}
