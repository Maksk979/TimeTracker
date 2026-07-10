using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Serilog;
using SkiaSharp;
using TimeTracker.Core.Storage;

namespace TimeTracker.App.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    private readonly ActivityRepository _repository;
    private readonly ILogger _log;

    private static readonly SKColor InkMutedColor = SKColor.Parse("#A0A4AC");
    private static readonly SKColor AccentColor = SKColor.Parse("#6C72CB");

    [ObservableProperty] private DateTime _dateFrom = DateTime.Today.AddDays(-7);
    [ObservableProperty] private DateTime _dateTo = DateTime.Today;
    [ObservableProperty] private string _totalActiveTime = "—";
    [ObservableProperty] private string _totalIdleTime = "—";
    [ObservableProperty] private int _totalSessions;

    public ISeries[] CategoryPieSeries { get; set; } = [];
    public ISeries[] DailyBarSeries { get; set; } = [];
    public Axis[] DailyXAxes { get; set; } = [];
    public Axis[] DailyYAxes { get; set; } = [];

    public ReportsViewModel(ActivityRepository repository, ILogger log)
    {
        _repository = repository;
        _log = log;
    }

    [RelayCommand]
    private async Task GenerateAsync()
    {
        try
        {
            var sessions = await _repository.GetSessionsForRangeAsync(DateFrom, DateTo);
            var categoryTotals = await _repository.GetCategoryTotalsForRangeAsync(DateFrom, DateTo);
            var dailyTotals = await _repository.GetDailyTotalsForRangeAsync(DateFrom, DateTo);

            var activeSeconds = sessions.Where(s => !s.IsIdle).Sum(s => s.DurationSeconds);
            var idleSeconds = sessions.Where(s => s.IsIdle).Sum(s => s.DurationSeconds);
            TotalActiveTime = FormatDuration(activeSeconds);
            TotalIdleTime = FormatDuration(idleSeconds);
            TotalSessions = sessions.Count;

            var labelPaint = new SolidColorPaint(InkMutedColor);

            CategoryPieSeries = categoryTotals
                .OrderByDescending(kv => kv.Value)
                .Select(kv => new PieSeries<long>
                {
                    Values = [kv.Value],
                    Name = kv.Key,
                    Fill = new SolidColorPaint(SKColor.Parse(GetCategoryColor(kv.Key))),
                })
                .ToArray();

            var days = dailyTotals.OrderBy(kv => kv.Key).ToList();
            DailyXAxes =
            [
                new Axis
                {
                    Labels = days.Select(d => d.Key.ToString("dd.MM")).ToArray(),
                    LabelsRotation = 45,
                    LabelsPaint = labelPaint,
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#2A2D34")),
                }
            ];

            DailyYAxes =
            [
                new Axis
                {
                    LabelsPaint = labelPaint,
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#2A2D34")),
                }
            ];

            DailyBarSeries =
            [
                new ColumnSeries<long>
                {
                    Values = days.Select(d => d.Value).ToArray(),
                    Fill = new SolidColorPaint(AccentColor),
                }
            ];

            OnPropertyChanged(nameof(CategoryPieSeries));
            OnPropertyChanged(nameof(DailyBarSeries));
            OnPropertyChanged(nameof(DailyXAxes));
            OnPropertyChanged(nameof(DailyYAxes));
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не удалось сформировать отчёт");
        }
    }

    private static string FormatDuration(long seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        if (ts.TotalHours >= 24)
            return $"{(int)ts.TotalDays}д {ts.Hours}ч {ts.Minutes}м";
        return ts.TotalHours >= 1 ? $"{(int)ts.TotalHours}ч {ts.Minutes}м" : $"{ts.Minutes}м";
    }

    private static string GetCategoryColor(string categoryName)
    {
        return categoryName switch
        {
            "Разработка" => "#4C9F70",
            "Браузер" => "#3B82F6",
            "Общение" => "#A855F7",
            "Игры" => "#EF4444",
            "Офис и документы" => "#F59E0B",
            "Терминал" => "#6B7280",
            _ => "#9CA3AF",
        };
    }
}
