using TrackerOfflineSearch.Core.Interfaces;
using TrackerSearch.ViewModels;

namespace TrackerSearch.Dialogs.Import;

public class ImportWizardViewModel: ActivatableViewModel, IScreen
{
    public ImportWizardViewModel(
        IArchiveReader archiveReader,
        IIndexImportService importService
        )
    {
        CancelCommand = ReactiveCommand.CreateFromTask(ConfirmCancelAsync);

        _paramsPage = new ParametersViewModel(this);
        _progressPage = new ProgressViewModel(this, archiveReader, importService);
        _resultPage = new ResultViewModel(this);

        _paramsPage.GoNextCommand
            .Select(p => _progressPage.WithParameters(p))
            .InvokeCommand<IRoutableViewModel>(Router.Navigate);

        Observable.Merge(
            _paramsPage.CancelCommand,
            _progressPage.CancelCommand,
            _resultPage.CancelCommand
            )
            .InvokeCommand(CancelCommand);

        this.WhenActivated(d =>
        {
            Observable.Return(_paramsPage)
                .InvokeCommand<IRoutableViewModel>(Router.Navigate)
                .DisposeWith(d);
        });
    }

    public RoutingState Router { get; } = new();

    public ReactiveCommand<Unit, bool> CancelCommand { get; }

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
}
