using System.ComponentModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using TimeTracker.App.Services;

namespace TimeTracker.App;

/// <summary>
/// ViewModel для окна обновления.
/// </summary>
public partial class UpdateViewModel : ObservableObject
{
    private readonly UpdateService _updateService;
    private readonly GitHubRelease _release;
    private readonly ILogger _log;

    [ObservableProperty] private string _releaseName = string.Empty;
    [ObservableProperty] private string _versionInfo = string.Empty;
    [ObservableProperty] private string _releaseNotes = string.Empty;
    [ObservableProperty] private string _statusText = "Подготовка к скачиванию...";
    [ObservableProperty] private double _progressScale;
    [ObservableProperty] private double _progressWidth;
    [ObservableProperty] private bool _isDownloading;

    public UpdateViewModel(UpdateService updateService, GitHubRelease release, ILogger log)
    {
        _updateService = updateService;
        _release = release;
        _log = log;

        ReleaseName = release.Name;
        VersionInfo = $"Текущая: {_updateService.DisplayVersion} → Новая: {release.TagName}";
        ReleaseNotes = release.Body?.Length > 300 ? release.Body[..300] + "..." : release.Body ?? "";
    }

    [RelayCommand]
    private void Skip()
    {
        var window = System.Windows.Application.Current.Windows.OfType<UpdateWindow>().FirstOrDefault();
        window?.Close();
    }

    [RelayCommand]
    private async Task UpdateAsync()
    {
        if (IsDownloading) return;
        IsDownloading = true;

        try
        {
            StatusText = "Скачивание обновления...";

            var progress = new Progress<int>(percent =>
            {
                ProgressScale = percent / 100.0;
                ProgressWidth = 340;
                StatusText = $"Скачивание... {percent}%";
            });

            var tempPath = await _updateService.DownloadUpdateAsync(_release, progress);

            StatusText = "Установка обновления...";
            ProgressScale = 1.0;
            ProgressWidth = 340;

            await Task.Delay(500);

            _updateService.ApplyUpdate(tempPath);
            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Ошибка обновления");
            StatusText = $"Ошибка: {ex.Message}";
            IsDownloading = false;
        }
    }
}

/// <summary>
/// Окно обновления.
/// </summary>
public partial class UpdateWindow : Window
{
    public UpdateWindow(UpdateViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
