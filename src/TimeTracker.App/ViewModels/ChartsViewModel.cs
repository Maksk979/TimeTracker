using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Serilog;
using SkiaSharp;
using TimeTracker.Core.Storage;

namespace TimeTracker.App.ViewModels;

public partial class ChartsViewModel : ObservableObject
{
    private readonly ActivityRepository _repository;
    private readonly ILogger _log;

    private static readonly SKColor InkMutedColor = SKColor.Parse("#A0A4AC");
    private static readonly SKColor AccentColor = SKColor.Parse("#6C72CB");

    public ISeries[] CategoryPieSeries { get; set; } = [];
    public ISeries[] HourlyBarSeries { get; set; } = [];
    public Axis[] HourlyXAxes { get; set; } = [];
    public Axis[] HourlyYAxes { get; set; } = [];

    public ChartsViewModel(ActivityRepository repository, ILogger log)
    {
        _repository = repository;
        _log = log;

        var labelPaint = new SolidColorPaint(InkMutedColor);

        HourlyXAxes =
        [
            new Axis
            {
                Labels = Enumerable.Range(0, 24).Select(h => $"{h:00}").ToArray(),
                LabelsPaint = labelPaint,
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#2A2D34")),
            }
        ];

        HourlyYAxes =
        [
            new Axis
            {
                LabelsPaint = labelPaint,
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#2A2D34")),
            }
        ];
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            var categoryTotals = await _repository.GetCategoryTotalsForDayAsync(DateTime.Today);
            var hourlyTotals = await _repository.GetHourlyTotalsForDayAsync(DateTime.Today);

            CategoryPieSeries = categoryTotals
                .OrderByDescending(kv => kv.Value)
                .Select(kv => new PieSeries<long>
                {
                    Values = [kv.Value],
                    Name = kv.Key,
                    Fill = new SolidColorPaint(SKColor.Parse(GetCategoryColor(kv.Key))),
                })
                .ToArray();

            HourlyBarSeries =
            [
                new ColumnSeries<long>
                {
                    Values = hourlyTotals,
                    Fill = new SolidColorPaint(AccentColor),
                }
            ];

            OnPropertyChanged(nameof(CategoryPieSeries));
            OnPropertyChanged(nameof(HourlyBarSeries));
            OnPropertyChanged(nameof(HourlyXAxes));
            OnPropertyChanged(nameof(HourlyYAxes));
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не удалось загрузить данные для графиков");
        }
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
