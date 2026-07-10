using Microsoft.EntityFrameworkCore;
using TimeTracker.Core.Storage.Entities;

namespace TimeTracker.Core.Storage;

/// <summary>
/// Репозиторий для записи сессий и чтения агрегатов для отчётов.
/// Отделяет домен (ActivityTracker) от деталей EF Core.
/// </summary>
public sealed class ActivityRepository
{
    private readonly Func<TimeTrackerDbContext> _contextFactory;

    public ActivityRepository(Func<TimeTrackerDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>Сохраняет несколько сессий за один вызов (batch insert).</summary>
    public async Task SaveSessionsAsync(IEnumerable<ActivitySession> sessions, CancellationToken ct = default)
    {
        await using var db = _contextFactory();
        db.Sessions.AddRange(sessions);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Возвращает все сессии за указанный день (по локальному времени).</summary>
    public async Task<List<ActivitySession>> GetSessionsForDayAsync(DateTime localDay, CancellationToken ct = default)
    {
        var (startUtc, endUtc) = ToUtcDayRange(localDay);
        await using var db = _contextFactory();
        return await db.Sessions
            .AsNoTracking()
            .Include(s => s.Category)
            .Where(s => s.StartedAtUtc >= startUtc && s.StartedAtUtc < endUtc)
            .OrderBy(s => s.StartedAtUtc)
            .ToListAsync(ct);
    }

    /// <summary>Суммарное время по категориям за указанный день (для круговой диаграммы).</summary>
    public async Task<Dictionary<string, long>> GetCategoryTotalsForDayAsync(DateTime localDay, CancellationToken ct = default)
    {
        var (startUtc, endUtc) = ToUtcDayRange(localDay);
        await using var db = _contextFactory();

        var rows = await db.Sessions
            .AsNoTracking()
            .Where(s => s.StartedAtUtc >= startUtc && s.StartedAtUtc < endUtc && !s.IsIdle)
            .GroupBy(s => new { CatId = (int?)s.CategoryId!, CatName = s.Category != null ? s.Category.Name : "Прочее" })
            .Select(g => new { g.Key.CatName, Total = g.Sum(s => s.DurationSeconds) })
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.CatName, r => r.Total);
    }

    /// <summary>Время активности по часам дня (для столбчатой диаграммы).</summary>
    public async Task<long[]> GetHourlyTotalsForDayAsync(DateTime localDay, CancellationToken ct = default)
    {
        var (startUtc, endUtc) = ToUtcDayRange(localDay);
        await using var db = _contextFactory();

        var totals = new long[24];
        var rows = await db.Sessions
            .AsNoTracking()
            .Where(s => s.StartedAtUtc >= startUtc && s.StartedAtUtc < endUtc && !s.IsIdle)
            .Select(s => new { LocalHour = s.StartedAtUtc.ToLocalTime().Hour, s.DurationSeconds })
            .ToListAsync(ct);

        foreach (var r in rows)
        {
            totals[r.LocalHour] += r.DurationSeconds;
        }

        return totals;
    }

    private static (DateTime startUtc, DateTime endUtc) ToUtcDayRange(DateTime localDay)
    {
        var date = localDay.Date;
        var startLocal = date;
        var endLocal = date.AddDays(1);
        // ToUniversalTime учитывает текущий часовой пояс машины.
        return (startLocal.ToUniversalTime(), endLocal.ToUniversalTime());
    }

    /// <summary>Сессии за период (по локальному времени).</summary>
    public async Task<List<ActivitySession>> GetSessionsForRangeAsync(DateTime startLocal, DateTime endLocal, CancellationToken ct = default)
    {
        var startUtc = startLocal.Date.ToUniversalTime();
        var endUtc = endLocal.Date.AddDays(1).ToUniversalTime();
        await using var db = _contextFactory();
        return await db.Sessions
            .AsNoTracking()
            .Include(s => s.Category)
            .Where(s => s.StartedAtUtc >= startUtc && s.StartedAtUtc < endUtc)
            .OrderBy(s => s.StartedAtUtc)
            .ToListAsync(ct);
    }

    /// <summary>Суммарное время по категориям за период.</summary>
    public async Task<Dictionary<string, long>> GetCategoryTotalsForRangeAsync(DateTime startLocal, DateTime endLocal, CancellationToken ct = default)
    {
        var startUtc = startLocal.Date.ToUniversalTime();
        var endUtc = endLocal.Date.AddDays(1).ToUniversalTime();
        await using var db = _contextFactory();

        var rows = await db.Sessions
            .AsNoTracking()
            .Where(s => s.StartedAtUtc >= startUtc && s.StartedAtUtc < endUtc && !s.IsIdle)
            .GroupBy(s => s.Category != null ? s.Category.Name : "Прочее")
            .Select(g => new { CatName = g.Key, Total = g.Sum(s => s.DurationSeconds) })
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.CatName, r => r.Total);
    }

    /// <summary>Суммарное время по дням за период.</summary>
    public async Task<Dictionary<DateTime, long>> GetDailyTotalsForRangeAsync(DateTime startLocal, DateTime endLocal, CancellationToken ct = default)
    {
        var startUtc = startLocal.Date.ToUniversalTime();
        var endUtc = endLocal.Date.AddDays(1).ToUniversalTime();
        await using var db = _contextFactory();

        var rows = await db.Sessions
            .AsNoTracking()
            .Where(s => s.StartedAtUtc >= startUtc && s.StartedAtUtc < endUtc && !s.IsIdle)
            .Select(s => new { Day = s.StartedAtUtc.ToLocalTime().Date, s.DurationSeconds })
            .ToListAsync(ct);

        var result = new Dictionary<DateTime, long>();
        foreach (var r in rows)
        {
            if (result.ContainsKey(r.Day))
                result[r.Day] += r.DurationSeconds;
            else
                result[r.Day] = r.DurationSeconds;
        }
        return result;
    }
}
