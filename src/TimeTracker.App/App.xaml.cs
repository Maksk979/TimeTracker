using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TimeTracker.App.Helpers;
using TimeTracker.App.Services;
using TimeTracker.Core;
using TimeTracker.Core.Tracking;

namespace TimeTracker.App;

public partial class App : System.Windows.Application
{
    private static readonly Mutex SingleInstanceMutex = new(
        false,
        "Global\\TimeTracker_SingleInstance_8F3A2C71-2D4B-4E5F-9A1C-7B3E0D2F5A88");

    private ServiceProvider? _services;
    private ActivityTracker? _tracker;
    private System.Windows.Forms.NotifyIcon? _notifyIcon;
    private bool _isPaused;

    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TimeTracker");

    public static ILogger AppLog { get; private set; } = Serilog.Log.Logger;

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Обработка режима обновления: копируем новый exe поверх старого
        var args = Environment.GetCommandLineArgs();
        if (args.Length >= 3 && args[1] == "--update")
        {
            HandleUpdate(args[2]);
            return;
        }

        if (!SingleInstanceMutex.WaitOne(0, false))
        {
            System.Windows.MessageBox.Show("TimeTracker уже запущен.", "TimeTracker",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        ConfigureLogging();
        AppLog.Information("=== TimeTracker запуск ===");

        try
        {
            await InitializeAsync();
            StartTracking();
            SetupTray();
            ShowMainWindow();

            // Проверка обновлений в фоне
            _ = CheckForUpdatesAsync();
        }
        catch (Exception ex)
        {
            AppLog.Fatal(ex, "Критическая ошибка при запуске");
            System.Windows.MessageBox.Show($"Не удалось запустить TimeTracker:\n\n{ex.Message}",
                "Ошибка запуска", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    /// <summary>Режим обновления: заменяет старый exe новым и перезапускает.</summary>
    private void HandleUpdate(string currentExePath)
    {
        try
        {
            // Ждём пока старое приложение закроется
            Thread.Sleep(2000);

            var newExe = Environment.ProcessPath;
            if (newExe != null && File.Exists(newExe) && File.Exists(currentExePath))
            {
                File.Copy(newExe, currentExePath, overwrite: true);
                Process.Start(currentExePath);
            }
        }
        catch
        {
            // Тихо обновляем — если не получилось, старое приложение продолжит работу
        }

        Environment.Exit(0);
    }

    /// <summary>Проверяет обновления и показывает диалог.</summary>
    private async Task CheckForUpdatesAsync()
    {
        try
        {
            using var updateService = new UpdateService(AppLog);
            var release = await updateService.CheckForUpdateAsync();

            if (release != null)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    var viewModel = new UpdateViewModel(updateService, release, AppLog);
                    var window = new UpdateWindow(viewModel);
                    window.Owner = MainWindow;
                    window.Show();
                });
            }
        }
        catch (Exception ex)
        {
            AppLog.Warning(ex, "Ошибка проверки обновлений");
        }
    }

    private void ConfigureLogging()
    {
        Directory.CreateDirectory(DataDirectory);
        var logPath = Path.Combine(DataDirectory, "logs", "timetracker-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
            .CreateLogger();

        AppLog = Log.Logger;
    }

    private async Task InitializeAsync()
    {
        var host = new TrackerHost(DataDirectory, AppLog);
        _services = await host.BuildAsync();
    }

    private void StartTracking()
    {
        _tracker = _services!.GetRequiredService<ActivityTracker>();
        _tracker.Start();
    }

    private void SetupTray()
    {
        _notifyIcon = new System.Windows.Forms.NotifyIcon();
        _notifyIcon.Icon = AppIconGenerator.CreateIcon(16);
        _notifyIcon.Text = "TimeTracker";
        _notifyIcon.Visible = true;

        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        contextMenu.Items.Add("Открыть", null, (_, _) => ShowMain());
        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        contextMenu.Items.Add("Пауза", null, (_, _) => TogglePause());
        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        contextMenu.Items.Add("Выход", null, (_, _) =>
        {
            _notifyIcon.Visible = false;
            Shutdown();
        });

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (_, _) => ShowMain();
    }

    private void TogglePause()
    {
        _isPaused = !_isPaused;
        _tracker?.SetPaused(_isPaused);

        if (_notifyIcon != null)
        {
            _notifyIcon.Icon = _isPaused
                ? CreateGrayIcon(16)
                : AppIconGenerator.CreateIcon(16);
            _notifyIcon.Text = _isPaused ? "TimeTracker — пауза" : "TimeTracker";
        }
    }

    private static System.Drawing.Icon CreateGrayIcon(int size)
    {
        var bmp = new System.Drawing.Bitmap(size, size);
        using var g = System.Drawing.Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(System.Drawing.Color.Transparent);
        using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(120, 120, 130));
        g.FillEllipse(brush, 1, 1, size - 2, size - 2);
        return System.Drawing.Icon.FromHandle(bmp.GetHicon());
    }

    private void ShowMainWindow()
    {
        var window = new MainWindow(_services!, AppLog);
        MainWindow = window;
        window.Show();
    }

    public void ShowMain()
    {
        if (MainWindow == null)
        {
            ShowMainWindow();
        }
        else
        {
            MainWindow.Show();
            MainWindow.WindowState = WindowState.Normal;
            MainWindow.Activate();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tracker?.Dispose();
        _notifyIcon?.Dispose();
        _services?.Dispose();
        AppLog.Information("=== TimeTracker выход ===");
        Serilog.Log.CloseAndFlush();
        SingleInstanceMutex.ReleaseMutex();
        base.OnExit(e);
    }
}
