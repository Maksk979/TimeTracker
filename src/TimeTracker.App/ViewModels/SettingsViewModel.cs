using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using TimeTracker.Core.Configuration;

namespace TimeTracker.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsStore _store;
    private readonly TrackerSettings _settings;
    private readonly ILogger _log;

    [ObservableProperty] private int _pollIntervalMs;
    [ObservableProperty] private int _idleThresholdSeconds;
    [ObservableProperty] private int _minSessionSeconds;
    [ObservableProperty] private bool _storeWindowTitles;

    public SettingsViewModel(SettingsStore store, TrackerSettings settings, ILogger log)
    {
        _store = store;
        _settings = settings;
        _log = log;

        PollIntervalMs = settings.PollIntervalMs;
        IdleThresholdSeconds = settings.IdleThresholdSeconds;
        MinSessionSeconds = settings.MinSessionSeconds;
        StoreWindowTitles = settings.StoreWindowTitles;
    }

    [RelayCommand]
    private void Save()
    {
        _settings.PollIntervalMs = PollIntervalMs;
        _settings.IdleThresholdSeconds = IdleThresholdSeconds;
        _settings.MinSessionSeconds = MinSessionSeconds;
        _settings.StoreWindowTitles = StoreWindowTitles;

        _store.Save(_settings);
        _log.Information("Настройки сохранены");
    }
}
