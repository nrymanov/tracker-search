using TrackerOfflineSearch.Core.Interfaces;
using TrackerSearch.ViewModels;

namespace TrackerSearch.Dialogs.Import;

public class ImportWizardViewModel : ActivatableViewModel, IScreen
{
    public ImportWizardViewModel(
        IArchiveReader archiveReader,
        IIndexService indexService
        )
    {
        CancelCommand = ReactiveCommand.CreateFromTask(ConfirmCancelAsync);

        _paramsPage = new ParametersViewModel(this);
        _progressPage = new ProgressViewModel(this, archiveReader, indexService);
        _resultPage = new ResultViewModel(this);
        _errorPage = new ErrorViewModel(this);

        _paramsPage.GoNextCommand
            .Select(p => _progressPage.WithParameters(p))
            .InvokeCommand<IRoutableViewModel>(Router.Navigate);

        _progressPage.ImportCommand
            .OfType<ImportCompletedResult>()
            .Select(p => _resultPage.WithParameters(p))
            .InvokeCommand<IRoutableViewModel>(Router.Navigate);

        _progressPage.ImportCommand
            .OfType<ImportFailedResult>()
            .Select(p => _errorPage.WithParameters(p))
            .InvokeCommand<IRoutableViewModel>(Router.Navigate);

        Observable.Merge(
            _paramsPage.CancelCommand,
            _progressPage.CancelCommand,
            _resultPage.CancelCommand,
            _errorPage.CancelCommand
            )
            .InvokeCommand(CancelCommand);

        this.WhenActivated(d => 
            Observable.Return(_paramsPage)
                .InvokeCommand<IRoutableViewModel>(Router.Navigate)
                .DisposeWith(d)
        );
    }

    #region IScreen

    public RoutingState Router { get; } = new();

    #endregion

    #region Public

    public ReactiveCommand<Unit, bool> CancelCommand { get; }

    #endregion

    #region Private

    private async Task<bool> ConfirmCancelAsync()
    {
        var current = await Router.CurrentViewModel.Take(1);

        if (current is IWizardPageViewModel vm)
        {
            return await vm.ConfirmCancelAsync().ConfigureAwait(true);
        }

        return true;
    }

    private readonly ParametersViewModel _paramsPage;
    private readonly ProgressViewModel _progressPage;
    private readonly ResultViewModel _resultPage;
    private readonly ErrorViewModel _errorPage;

    #endregion
}
