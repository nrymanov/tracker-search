using Microsoft.Extensions.DependencyInjection;
using TrackerSearch.Dialogs.About;
using TrackerSearch.Dialogs.Import;
using TrackerSearch.ViewModels;

namespace TrackerSearch.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(IServiceProvider serviceProvider) : this()
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        this.WhenActivated(d => {

            this.WhenAnyValue(x => x.ViewModel!.SelectedPostContent)
                .Subscribe(content => PostContentView.LoadHtml(content))
                .DisposeWith(d);

            ViewModel!.Import.RegisterHandler(HandleImportAsync)
                .DisposeWith(d);

            ViewModel!.About.RegisterHandler(HandleAboutAsync)
                .DisposeWith(d);
        });
    }

    private async Task HandleImportAsync(IInteractionContext<Unit, bool> interaction)
    {
        var dialog = new ImportWizard
        {
            ViewModel = _serviceProvider.GetRequiredService<ImportWizardViewModel>(),
        };

        var result = await dialog.ShowDialog<bool>(this).ConfigureAwait(false);

        interaction.SetOutput(result);
    }

    private async Task HandleAboutAsync(IInteractionContext<Unit, Unit> interaction)
    {
        var dialog = new AboutDialog
        {
            ViewModel = _serviceProvider.GetRequiredService<AboutViewModel>(),
        };

        await dialog.ShowDialog(this).ConfigureAwait(false);

        interaction.SetOutput(output: Unit.Default);
    }

    private readonly IServiceProvider _serviceProvider;
}
