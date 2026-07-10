using Microsoft.EntityFrameworkCore;
using Serilog;
using TimeTracker.Core.Categorization;
using TimeTracker.Core.Storage.Entities;

namespace TimeTracker.Core.Storage;

/// <summary>
/// Создаёт SQLite-БД при первом запуске и наполняет её стандартными категориями/правилами.
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// Применяет миграции (или создаёт БД) и заполняет дефолтные данные, если БД пустая.
    /// </summary>
    public static async Task InitializeAsync(TimeTrackerDbContext db, ILogger log)
    {
        await db.Database.MigrateAsync();

        // Если категорий ещё нет — добавляем стартовый набор.
        if (!await db.Categories.AnyAsync())
        {
            log.Information("БД пустая — добавляем категории по умолчанию");
            var categories = DefaultRules.Build();
            db.Categories.AddRange(categories);
            await db.SaveChangesAsync();
            log.Information("Добавлено категорий: {Count}", categories.Count);
        }
    }
}
