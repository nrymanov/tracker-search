namespace TrackerSearch.Dialogs.Import;

public class ImportWizardViewLocator : IViewLocator
{
    public IViewFor? ResolveView<T>(T? viewModel, string? contract = null)
        => viewModel switch
        {
            //Login.LoginViewModel => new Views.Login.LoginView(),
            //Catalogs.CatalogListViewModel => new Views.Catalogs.CatalogListView(),
            //Shortcuts.ShortcutListViewModel => new Views.Shortcuts.ShortcutListView(),
            ParametersViewModel => new ParametersView(),
            ProgressViewModel => new ProgressView(),
            ResultViewModel => new ResultView(),

            _ => throw new ArgumentOutOfRangeException(nameof(viewModel)),
        };
}
