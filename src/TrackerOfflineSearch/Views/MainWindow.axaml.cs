using Microsoft.Extensions.DependencyInjection;
using TrackerOfflineSearch.Dialogs.About;
using TrackerOfflineSearch.Dialogs.Import;
using TrackerOfflineSearch.ViewModels;

namespace TrackerOfflineSearch.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    /// <summary>
    /// Parameterless constructor used exclusively by Avalonia Designer/Previewer.
    /// Should not be called at runtime - use MainWindow(IServiceProvider) instead.
    /// </summary>
    /// <remarks>
    /// WARNING: This constructor bypasses normal dependency injection.
    /// </remarks>
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Main constructor for application use with proper dependency injection.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency resolution</param>
    /// <exception cref="ArgumentNullException">Thrown when serviceProvider is null</exception>
    public MainWindow(IServiceProvider serviceProvider) : this()
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(x => x.ViewModel!.SelectedPostInfo)
                .Subscribe(postInfo => PostContentView.LoadHtml(postInfo?.Content ?? ""))
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

    private readonly IServiceProvider _serviceProvider = null!;
}
