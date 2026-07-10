using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using TimeTracker.Core.Storage;
using TimeTracker.Core.Storage.Entities;

namespace TimeTracker.App.ViewModels;

public partial class TodayViewModel : ObservableObject
{
    private readonly ActivityRepository _repository;
    private readonly ILogger _log;

    [ObservableProperty] private string _dateLabel = DateTime.Today.ToString("dddd, d MMMM yyyy");
    [ObservableProperty] private string _activeTime = "—";
    [ObservableProperty] private string _idleTime = "—";
    [ObservableProperty] private string _switchCount = "—";
    public ObservableCollection<SessionRow> Sessions { get; } = new();

    public TodayViewModel(ActivityRepository repository, ILogger log)
    {
        _repository = repository;
        _log = log;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            var sessions = await _repository.GetSessionsForDayAsync(DateTime.Today);

            var activeSeconds = sessions.Where(s => !s.IsIdle).Sum(s => s.DurationSeconds);
            var idleSeconds = sessions.Where(s => s.IsIdle).Sum(s => s.DurationSeconds);
            var switches = sessions.Count;

            ActiveTime = FormatDuration(activeSeconds);
            IdleTime = FormatDuration(idleSeconds);
            SwitchCount = switches.ToString();

            Sessions.Clear();
            foreach (var s in sessions)
                Sessions.Add(SessionRow.From(s));
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не удалось загрузить данные за сегодня");
        }
    }

    private static string FormatDuration(long seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalHours >= 1 ? $"{(int)ts.TotalHours}ч {ts.Minutes}м" : $"{ts.Minutes}м";
    }
}

public sealed class SessionRow
{
    public DateTime StartedAtLocal { get; init; }
    public string ProcessName { get; init; } = string.Empty;
    public string WindowTitle { get; init; } = string.Empty;
    public string DurationFormatted { get; init; } = string.Empty;
    public bool IsIdle { get; init; }
    public CategoryRef Category { get; init; } = new();

    public static SessionRow From(ActivitySession s) => new()
    {
        StartedAtLocal = s.StartedAtLocal,
        ProcessName = s.ProcessName,
        WindowTitle = s.WindowTitle,
        DurationFormatted = Format(s.DurationSeconds),
        IsIdle = s.IsIdle,
        Category = new CategoryRef { Name = s.Category?.Name ?? "—" },
    };

    private static string Format(long seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}ч{ts.Minutes:00}м"
            : $"{ts.Minutes}м{ts.Seconds:00}с";
    }
}

public sealed class CategoryRef
{
    public string? Name { get; init; }
}
