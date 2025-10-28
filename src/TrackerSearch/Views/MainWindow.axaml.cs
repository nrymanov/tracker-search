using TrackerSearch.ViewModels;

namespace TrackerSearch.Views;
public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();

        this.WhenActivated(d => {

            this.WhenAnyValue(x => x.ViewModel!.SelectedPostContent)
                .Subscribe(content => PostContentView.LoadHtml(content)                )
                .DisposeWith(d);
        });
    }
}
