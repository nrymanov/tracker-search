using Microsoft.Extensions.Logging;
using Prism.Events;
using ReactiveUI;

namespace TrackerOfflineSearch.ViewModels;

public abstract class ViewModelBase<T> : ReactiveObject
{
    protected ViewModelBase(IEventAggregator eventAggregator, ILogger<T> logger)
    {
        this._eventAggregator = eventAggregator ?? throw new System.ArgumentNullException(nameof(eventAggregator));
        this._logger = logger ?? throw new System.ArgumentNullException(nameof(logger));

        this.LogDebug("{class} created", typeof(T).Name);
    }

    private string? _title = string.Empty;
    public string? Title
    {
        get => this._title;
        protected set => this.RaiseAndSetIfChanged(ref this._title, value);
    }

    private readonly IEventAggregator _eventAggregator;
    protected IEventAggregator EventAggregator => this._eventAggregator;

    private readonly ILogger<T> _logger;
    protected ILogger<T> Logger => this._logger;

    protected void LogDebug(string? message, params object?[] args)
    {
        this.Logger.LogDebug(message, args);
    }
}
