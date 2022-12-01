using System;
using ReactiveUI;
using TrackerOfflineSearch.Services;

namespace TrackerOfflineSearch.ViewModels;

public class DateIntervalViewModel : ReactiveObject, IDateInterval
{
    public DateIntervalViewModel(DateIntervalKind kind)
    {
        this.Kind = kind;

        var utcNow = DateTime.UtcNow;
        this.Dates = this.Kind switch
        {
            DateIntervalKind.None => (null, null),
            DateIntervalKind.Week => ((DateTime?, DateTime?))(utcNow.AddDays(-7), utcNow),
            DateIntervalKind.TwoWeeks => ((DateTime?, DateTime?))(utcNow.AddDays(-14), utcNow),
            DateIntervalKind.Month => ((DateTime?, DateTime?))(utcNow.AddMonths(-1), utcNow),
            DateIntervalKind.Quarter => ((DateTime?, DateTime?))(utcNow.AddMonths(-3), utcNow),
            DateIntervalKind.HalfYear => ((DateTime?, DateTime?))(utcNow.AddMonths(-6), utcNow),
            DateIntervalKind.Year => ((DateTime?, DateTime?))(utcNow.AddYears(-1), utcNow),
            _ => throw new NotSupportedException()
        };
        this.UpdateTitle();
    }

    public DateIntervalKind Kind
    {
        get => this._kind;
        private set => this.RaiseAndSetIfChanged(ref this._kind, value);
    }

    public (DateTime?, DateTime?) Dates
    {
        get => this._dates;
        private set => this.RaiseAndSetIfChanged(ref this._dates, value);
    }

    public string Title
    {
        get => this._title;
        private set => this.RaiseAndSetIfChanged(ref this._title, value);
    }

    private void UpdateTitle()
    {
        this.Title = this.Kind switch
        {
            DateIntervalKind.None => "Any time",
            DateIntervalKind.Week => "Last week",
            DateIntervalKind.TwoWeeks => "Last two week",
            DateIntervalKind.Month => "Last month",
            DateIntervalKind.Quarter => "Last quarter",
            DateIntervalKind.HalfYear => "Last six months",
            DateIntervalKind.Year => "Last year",
            _ => throw new NotSupportedException(),
        };
    }

    private DateIntervalKind _kind;
    private (DateTime?, DateTime?) _dates;
    private string _title;
}
