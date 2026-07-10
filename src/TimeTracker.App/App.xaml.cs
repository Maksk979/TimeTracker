using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
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
        }
        catch (Exception ex)
        {
            AppLog.Fatal(ex, "Критическая ошибка при запуске");
            System.Windows.MessageBox.Show($"Не удалось запустить TimeTracker:\n\n{ex.Message}",
                "Ошибка запуска", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
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
        _notifyIcon.Icon = CreateTrayIcon(System.Drawing.Color.FromArgb(76, 159, 112));
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
                ? CreateTrayIcon(System.Drawing.Color.FromArgb(156, 163, 175))
                : CreateTrayIcon(System.Drawing.Color.FromArgb(76, 159, 112));
            _notifyIcon.Text = _isPaused ? "TimeTracker — пауза" : "TimeTracker";
        }
    }

    private static System.Drawing.Icon CreateTrayIcon(System.Drawing.Color color, int size = 16)
    {
        var bmp = new System.Drawing.Bitmap(size, size);
        using var g = System.Drawing.Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(System.Drawing.Color.Transparent);
        using var brush = new System.Drawing.SolidBrush(color);
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
