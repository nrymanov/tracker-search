using System.Windows;
using Prism.Dialogs;
using TrackerOfflineSearch.Helpers;
using TrackerOfflineSearch.Settings;

namespace TrackerOfflineSearch.ForumSelector;
/// <summary>
/// Interaction logic for ForumSelectorWindow.xaml
/// </summary>
public partial class ForumSelectorWindow : Window, IDialogWindow
{
    public ForumSelectorWindow(IPlacement<ForumSelectorWindow> placement)
    {
        this.InitializeComponent();

        this.SourceInitialized += (s, e) => this.HideMinimizeAndMaximizeButtons();

        this.placement = placement ?? throw new System.ArgumentNullException(nameof(placement));
        this.placement.Attach(this);
    }
    public IDialogResult Result { get; set; }

    private readonly IPlacement<ForumSelectorWindow> placement;
}
