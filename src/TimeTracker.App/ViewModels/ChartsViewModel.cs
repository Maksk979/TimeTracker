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

    public ISeries[] CategoryPieSeries { get; set; } = [];
    public ISeries[] HourlyBarSeries { get; set; } = [];
    public Axis[] HourlyXAxes { get; set; } = [];

    public ChartsViewModel(ActivityRepository repository, ILogger log)
    {
        _repository = repository;
        _log = log;
        HourlyXAxes =
        [
            new Axis
            {
                Labels = Enumerable.Range(0, 24).Select(h => $"{h:00}:00").ToArray(),
                LabelsRotation = 15,
                TextSize = 10,
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
                    Fill = new SolidColorPaint(SKColors.MediumPurple),
                    MaxBarWidth = 16,
                }
            ];

            OnPropertyChanged(nameof(CategoryPieSeries));
            OnPropertyChanged(nameof(HourlyBarSeries));
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
