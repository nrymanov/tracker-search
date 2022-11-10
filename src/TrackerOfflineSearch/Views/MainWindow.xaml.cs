using System.Windows;
using Prism.Regions;

namespace TrackerOfflineSearch.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IRegionManager regionManager;

    public MainWindow(IRegionManager regionManager)
    {
        InitializeComponent();

        this.regionManager = regionManager;
    }
}
