using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TimeTracker.Core.Configuration;
using TimeTracker.Core.Storage;
using TimeTracker.Core.Tracking;

namespace TimeTracker.Core;

/// <summary>
/// Точка компоновки Core: настраивает DI-контейнер, БД, настройки и репозитории.
/// WPF-приложение вызывает <see cref="BuildAsync"/> на старте.
/// </summary>
public sealed class TrackerHost : IAsyncDisposable
{
    private readonly string _dataDirectory;
    private readonly ILogger _log;
    private ServiceProvider? _services;

    public TrackerHost(string dataDirectory, ILogger log)
    {
        _dataDirectory = dataDirectory;
        _log = log;
    }

    /// <summary>Собирает контейнер, инициализирует БД и сидинг. Возвращает ServiceProvider.</summary>
    public async Task<ServiceProvider> BuildAsync()
    {
        Directory.CreateDirectory(_dataDirectory);
        var dbPath = Path.Combine(_dataDirectory, "timetracker.db");
        var settingsPath = Path.Combine(_dataDirectory, "settings.json");

        _log.Information("Папка данных: {Dir}", _dataDirectory);

        var services = new ServiceCollection();

        // DbContext — регистрируем как factory, чтобы каждый потребитель получал
        // короткоживущий контекст (EF Core не thread-safe для общего использования).
        services.AddDbContext<TimeTrackerDbContext>(opts =>
            opts.UseSqlite($"Data Source={dbPath}"));

        // Явно регистрируем фабрику для Func<TimeTrackerDbContext>.
        services.AddSingleton(CreateContextFactory(dbPath));

        services.AddSingleton(_log);
        services.AddSingleton(new SettingsStore(settingsPath, _log));
        services.AddSingleton(sp =>
        {
            var store = sp.GetRequiredService<SettingsStore>();
            return store.Load();
        });

        services.AddSingleton<ActivityRepository>();
        services.AddSingleton<ActivityTracker>();

        _services = services.BuildServiceProvider();

        // Инициализация БД.
        await using (var scope = _services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TimeTrackerDbContext>();
            await DatabaseInitializer.InitializeAsync(db, _log);
        }

        return _services;
    }

    /// <summary>Factory для создания короткоживущего DbContext вне DI (напр. в репозитории).</summary>
    public static Func<TimeTrackerDbContext> CreateContextFactory(string dbPath)
    {
        var options = new DbContextOptionsBuilder<TimeTrackerDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        return () => new TimeTrackerDbContext(options);
    }

    public async ValueTask DisposeAsync()
    {
        if (_services != null)
        {
            await _services.DisposeAsync();
        }
    }
}
