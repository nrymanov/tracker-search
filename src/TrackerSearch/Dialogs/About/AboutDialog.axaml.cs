namespace TrackerSearch.Dialogs.About;

public partial class AboutDialog : ReactiveWindow<AboutViewModel>
{
    public AboutDialog()
    {
        InitializeComponent();
    }

    private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();
}
