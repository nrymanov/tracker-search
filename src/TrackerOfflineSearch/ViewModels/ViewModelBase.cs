using Microsoft.Extensions.Logging;
using Prism.Events;
using ReactiveUI;

namespace TrackerOfflineSearch.ViewModels;

public abstract class ViewModelBase<T> : ReactiveObject
{
    #region Constructor

    protected ViewModelBase(IEventAggregator eventAggregator, ILogger<T> logger)
    {
        this.eventAggregator = eventAggregator ?? throw new System.ArgumentNullException(nameof(eventAggregator));
        this.logger = logger ?? throw new System.ArgumentNullException(nameof(logger));

        this.Logger.LogDebug("{class} created", typeof(T).Name);
    }

    #endregion

    #region Public properties & methods

    private string? _title = string.Empty;
    public string? Title
    {
        get => this._title;
        protected set => this.RaiseAndSetIfChanged(ref this._title, value);
    }

    #endregion

    #region Protected properties & methods

    protected IEventAggregator EventAggregator => this.eventAggregator;

    protected ILogger<T> Logger => this.logger;

    #endregion

    #region Private properties & methods

    private readonly IEventAggregator eventAggregator;
    private readonly ILogger<T> logger;

    #endregion
}
