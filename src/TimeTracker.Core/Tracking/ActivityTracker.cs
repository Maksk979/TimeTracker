using Microsoft.EntityFrameworkCore;
using Serilog;
using TimeTracker.Core.Categorization;
using TimeTracker.Core.Configuration;
using TimeTracker.Core.Storage;
using TimeTracker.Core.Storage.Entities;
using TimeTracker.Core.Tracking.Win32;

namespace TimeTracker.Core.Tracking;

/// <summary>
/// Сердце трекера: опрашивает активное окно в фоновом потоке,
/// накапливает сессию и сбрасывает её в БД при смене окна или уходе в простой.
///
/// Потокобезопасность: методы Start/Stop/SetPaused можно вызывать из любого потока.
/// Состояние сессии меняется только из рабочего цикла (один writer).
/// </summary>
public sealed class ActivityTracker : IDisposable
{
    private readonly RuleEngine _ruleEngine = new();
    private readonly ActivityRepository _repository;
    private readonly Func<TimeTrackerDbContext> _contextFactory;
    private readonly TrackerSettings _settings;
    private readonly ILogger _log;

    private Thread? _thread;
    private CancellationTokenSource? _cts;

    // Текущая накапливаемая сессия (заполняется из рабочего потока).
    private ActiveWindowInfo? _currentWindow;
    private DateTime _currentWindowStartedUtc;
    private List<Category>? _categoriesCache;

    // События для UI (вызываются из рабочего потока — UI должен маршалить сам).
    public event Action<ActiveWindowInfo>? CurrentWindowChanged;
    public event Action<bool>? IsIdleChanged;

    private bool _isIdle;

    public ActivityTracker(
        ActivityRepository repository,
        Func<TimeTrackerDbContext> contextFactory,
        TrackerSettings settings,
        ILogger log)
    {
        _repository = repository;
        _contextFactory = contextFactory;
        _settings = settings;
        _log = log;
    }

    /// <summary>Запускает фоновый цикл опроса.</summary>
    public void Start()
    {
        if (_thread != null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _thread = new Thread(() => Run(_cts.Token))
        {
            IsBackground = true,
            Name = "ActivityTracker",
        };
        _thread.Start();
        _log.Information("ActivityTracker запущен");
    }

    /// <summary>Останавливает цикл и сбрасывает накопленную сессию в БД.</summary>
    public void Stop()
    {
        if (_cts == null)
        {
            return;
        }

        _cts.Cancel();
        _thread?.Join(TimeSpan.FromSeconds(5));
        _cts.Dispose();
        _cts = null;
        _thread = null;
        _log.Information("ActivityTracker остановлен");
    }

    /// <summary>
    /// Временно приостанавливает трекинг (пауза из трея).
    /// Текущая сессия закрывается и пишется в БД.
    /// </summary>
    public void SetPaused(bool paused)
    {
        _settings.IsTrackingEnabled = !paused;
        if (paused)
        {
            // Сбрасываем накопленную сессию, чтобы не приписать время паузы.
            FlushCurrentSession(DateTime.UtcNow, idle: true).GetAwaiter().GetResult();
        }
    }

    private void Run(CancellationToken ct)
    {
        // Предзагружаем категории для сопоставления правилами.
        _categoriesCache = LoadCategories();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                PollOnce();
            }
            catch (Exception ex)
            {
                // Один сбой не должен ронять трекинг.
                _log.Error(ex, "Ошибка в цикле опроса ActivityTracker");
            }

            try
            {
                Task.Delay(_settings.PollIntervalMs, ct).Wait(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        // Перед выходом сбрасываем накопленную сессию.
        FlushCurrentSession(DateTime.UtcNow, idle: _isIdle).GetAwaiter().GetResult();
    }

    private void PollOnce()
    {
        var nowUtc = DateTime.UtcNow;
        var idleSeconds = ActiveWindowReader.GetIdleSeconds();
        var isIdleNow = idleSeconds >= _settings.IdleThresholdSeconds;

        // Сигнализируем переход в/из простоя.
        if (isIdleNow != _isIdle)
        {
            _isIdle = isIdleNow;
            IsIdleChanged?.Invoke(isIdleNow);
        }

        // На паузе или в простое — сессию не накапливаем.
        if (!_settings.IsTrackingEnabled)
        {
            return;
        }

        if (isIdleNow)
        {
            // Пользователь простаивает — закрываем активную сессию как неактивную.
            if (_currentWindow != null)
            {
                FlushCurrentSession(nowUtc, idle: true).GetAwaiter().GetResult();
            }
            return;
        }

        var window = ActiveWindowReader.GetCurrent();
        if (window.WindowHandle == IntPtr.Zero)
        {
            return;
        }

        // Окно сменилось? (по процессу + заголовку — заголовок важен, т.к. в одном
        // процессе могут быть разные задачи, напр. вкладки браузера).
        if (IsSameWindow(_currentWindow, window))
        {
            // Накапливаем дальше — ничего не делаем, время считается по startedAt.
            return;
        }

        // Окно сменилось — закрываем старую сессию и начинаем новую.
        FlushCurrentSession(nowUtc, idle: false).GetAwaiter().GetResult();

        _currentWindow = window;
        _currentWindowStartedUtc = nowUtc;
        CurrentWindowChanged?.Invoke(window);
    }

    /// <summary>
    /// Записывает накопленную сессию в БД, если она достаточно длинная,
    /// и сбрасывает текущее окно.
    /// </summary>
    private async Task FlushCurrentSession(DateTime nowUtc, bool idle)
    {
        if (_currentWindow == null)
        {
            return;
        }

        var duration = (nowUtc - _currentWindowStartedUtc).TotalSeconds;

        // Короткие переключения не сохраняем.
        if (duration >= _settings.MinSessionSeconds)
        {
            var session = BuildSession(_currentWindow, _currentWindowStartedUtc, nowUtc, idle);
            try
            {
                await _repository.SaveSessionsAsync([session]);
                _log.Debug("Сессия сохранена: {Process} ({Duration:F0}s, idle={Idle})",
                    session.ProcessName, duration, idle);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Не удалось сохранить сессию");
            }
        }

        _currentWindow = null;
    }

    private ActivitySession BuildSession(ActiveWindowInfo window, DateTime start, DateTime end, bool idle)
    {
        var session = new ActivitySession
        {
            ProcessName = window.ProcessName,
            ExecutableName = window.ExecutableName,
            WindowTitle = _settings.StoreWindowTitles ? window.WindowTitle : string.Empty,
            StartedAtUtc = start,
            EndedAtUtc = end,
            DurationSeconds = (long)(end - start).TotalSeconds,
            IsIdle = idle,
        };

        // Сопоставляем категорию, если кэш категорий подгружен.
        if (_categoriesCache is { Count: > 0 })
        {
            var windowForCat = new WindowForCategorization(
                window.ProcessName,
                _settings.StoreWindowTitles ? window.WindowTitle : string.Empty,
                window.ExecutableName);

            var category = _ruleEngine.Match(windowForCat, _categoriesCache);
            if (category != null)
            {
                session.CategoryId = category.Id;
            }
        }

        return session;
    }

    private List<Category> LoadCategories()
    {
        try
        {
            using var db = _contextFactory();
            return db.Categories
                .AsNoTracking()
                .Include(c => c.Rules)
                .ToList();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Не удалось загрузить категории");
            return new List<Category>();
        }
    }

    /// <summary>Перезагружает кэш категорий (вызывается после редактирования правил в UI).</summary>
    public void RefreshCategories()
    {
        _categoriesCache = LoadCategories();
        _log.Information("Кэш категорий обновлён");
    }

    /// <summary>
    /// Два снимка окна считаются одним и тем же активным окном, если совпадают
    /// процесс и заголовок. Дескриптор HWND может меняться (напр. при пересоздании окна).
    /// </summary>
    private static bool IsSameWindow(ActiveWindowInfo? a, ActiveWindowInfo b)
    {
        if (a == null)
        {
            return false;
        }

        return string.Equals(a.ProcessName, b.ProcessName, StringComparison.OrdinalIgnoreCase)
               && string.Equals(a.WindowTitle, b.WindowTitle, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        Stop();
    }
}
