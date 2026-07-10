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

    private static readonly SKColor InkColor = SKColor.Parse("#F0F0F2");
    private static readonly SKColor InkMutedColor = SKColor.Parse("#A0A4AC");
    private static readonly SKColor AccentColor = SKColor.Parse("#6C72CB");
    private static readonly SKColor HairlineColor = SKColor.Parse("#2A2D34");

    public ISeries[] CategoryPieSeries { get; set; } = [];
    public ISeries[] HourlyBarSeries { get; set; } = [];
    public Axis[] HourlyXAxes { get; set; } = [];
    public Axis[] HourlyYAxes { get; set; } = [];

    public SolidColorPaint LegendTextPaint { get; } = new(InkColor);
    public SolidColorPaint LegendNamePaint { get; } = new(InkMutedColor);

    public ChartsViewModel(ActivityRepository repository, ILogger log)
    {
        _repository = repository;
        _log = log;

        var labelPaint = new SolidColorPaint(InkColor) { FontFamily = "Segoe UI" };
        var separatorPaint = new SolidColorPaint(HairlineColor);

        HourlyXAxes =
        [
            new Axis
            {
                Name = "Час дня",
                NamePaint = new SolidColorPaint(InkMutedColor),
                Labels = Enumerable.Range(0, 24).Select(h => $"{h:00}:00").ToArray(),
                LabelsPaint = labelPaint,
                LabelsRotation = 30,
                SeparatorsPaint = separatorPaint,
                TextSize = 11,
                NameTextSize = 12,
            }
        ];

        HourlyYAxes =
        [
            new Axis
            {
                Name = "Секунды",
                NamePaint = new SolidColorPaint(InkMutedColor),
                LabelsPaint = labelPaint,
                SeparatorsPaint = separatorPaint,
                TextSize = 11,
                NameTextSize = 12,
                Labeler = value => FormatSeconds((long)value),
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
                    Name = "Активность",
                    MaxBarWidth = 20,
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

    private static string FormatSeconds(long seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
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
