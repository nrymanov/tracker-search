using System.Windows;
using TrackerOfflineSearch.Settings;

namespace TrackerOfflineSearch.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IPlacement<MainWindow> placement;

    public MainWindow(IPlacement<MainWindow> placement)
    {
        this.placement = placement ?? throw new System.ArgumentNullException(nameof(placement));

        this.InitializeComponent();

        this.placement.Attach(this);
    }
}
