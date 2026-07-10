using System.ComponentModel;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TimeTracker.App.Helpers;
using TimeTracker.App.ViewModels;
using TimeTracker.Core.Configuration;
using TimeTracker.Core.Storage;
using TimeTracker.Core.Tracking;

namespace TimeTracker.App;

public partial class MainWindow : Window
{
    private readonly TodayViewModel _todayVm;
    private readonly ChartsViewModel _chartsVm;
    private readonly RulesViewModel _rulesVm;
    private readonly SettingsViewModel _settingsVm;
    private readonly ReportsViewModel _reportsVm;

    public MainWindow(ServiceProvider services, ILogger log)
    {
        InitializeComponent();

        // Устанавливаем кастомную иконку
        var icon = AppIconGenerator.CreateIcon(256);
        Icon = AppIconGenerator.ToBitmapSource(icon);

        var repository = services.GetRequiredService<ActivityRepository>();
        var contextFactory = services.GetRequiredService<Func<TimeTrackerDbContext>>();
        var tracker = services.GetRequiredService<ActivityTracker>();
        var settingsStore = services.GetRequiredService<SettingsStore>();
        var settings = services.GetRequiredService<TrackerSettings>();

        _todayVm = new TodayViewModel(repository, log);
        _chartsVm = new ChartsViewModel(repository, log);
        _rulesVm = new RulesViewModel(contextFactory, tracker, log);
        _settingsVm = new SettingsViewModel(settingsStore, settings, log);
        _reportsVm = new ReportsViewModel(repository, log);

        TodayTab.DataContext = _todayVm;
        ChartsTab.DataContext = _chartsVm;
        RulesTab.DataContext = _rulesVm;
        SettingsTab.DataContext = _settingsVm;
        ReportsTab.DataContext = _reportsVm;

        Loaded += async (_, _) =>
        {
            await _todayVm.LoadCommand.ExecuteAsync(null);
            await _chartsVm.LoadCommand.ExecuteAsync(null);
            await _rulesVm.LoadCommand.ExecuteAsync(null);
        };
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
        base.OnClosing(e);
    }
}
