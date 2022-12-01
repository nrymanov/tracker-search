using Microsoft.Extensions.Logging;
using Prism.Events;
using ReactiveUI;

namespace TrackerOfflineSearch.ViewModels;

public abstract class ViewModelBase<T> : ReactiveObject
{
    protected ViewModelBase(IEventAggregator eventAggregator, ILogger<T> logger)
    {
        this.eventAggregator = eventAggregator ?? throw new System.ArgumentNullException(nameof(eventAggregator));
        this.logger = logger ?? throw new System.ArgumentNullException(nameof(logger));

        this.Logger.LogDebug("{class} created", typeof(T).Name);
    }

    private string? _title = string.Empty;
    public string? Title
    {
        get => this._title;
        protected set => this.RaiseAndSetIfChanged(ref this._title, value);
    }

    protected IEventAggregator EventAggregator => this.eventAggregator;

    protected ILogger<T> Logger => this.logger;

    private readonly IEventAggregator eventAggregator;
    private readonly ILogger<T> logger;
}
