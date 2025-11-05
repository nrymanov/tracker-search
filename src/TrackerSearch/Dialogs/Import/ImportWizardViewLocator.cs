namespace TrackerSearch.Dialogs.Import;

public class ImportWizardViewLocator : IViewLocator
{
    public IViewFor? ResolveView<T>(T? viewModel, string? contract = null)
        => viewModel switch
        {
            ParametersViewModel => new ParametersView(),
            ProgressViewModel => new ProgressView(),
            ResultViewModel => new ResultView(),
            ErrorViewModel => new ErrorView(),

            _ => throw new ArgumentOutOfRangeException(nameof(viewModel)),
        };
}
