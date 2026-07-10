using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TimeTracker.Core.Storage;

/// <summary>
/// Design-time factory для EF Core: позволяет команде "dotnet ef migrations add"
/// создать DbContext без DI-контейнера. В рантайме не используется.
/// </summary>
public sealed class TimeTrackerDbContextFactory
    : IDesignTimeDbContextFactory<TimeTrackerDbContext>
{
    public TimeTrackerDbContext CreateDbContext(string[] args)
    {
        // Путь к БД при создании миграций — временный, в реальном приложении
        // путь формируется в TrackerHost через папку данных пользователя.
        var options = new DbContextOptionsBuilder<TimeTrackerDbContext>()
            .UseSqlite("Data Source=timetracker.db")
            .Options;

        return new TimeTrackerDbContext(options);
    }
}
