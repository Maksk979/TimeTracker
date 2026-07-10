namespace TimeTracker.Core.Storage.Entities;

/// <summary>
/// Одна непрерывная сессия активности в одном приложении/окне.
/// Закрывается при смене активного окна или при уходе пользователя в простой.
/// </summary>
public sealed class ActivitySession
{
    public int Id { get; set; }

    /// <summary>Имя процесса, например "devenv" или "chrome".</summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>Имя исполняемого файла, если удалось получить (например "devenv.exe").</summary>
    public string? ExecutableName { get; set; }

    /// <summary>Заголовок окна на момент сессии (может быть пустым, если включён режим приватности).</summary>
    public string WindowTitle { get; set; } = string.Empty;

    /// <summary>UTC-время начала сессии.</summary>
    public DateTime StartedAtUtc { get; set; }

    /// <summary>UTC-время окончания сессии.</summary>
    public DateTime EndedAtUtc { get; set; }

    /// <summary>Длительность в секундах (кеш EndedAtUtc - StartedAtUtc для удобных запросов).</summary>
    public long DurationSeconds { get; set; }

    /// <summary>Сессия помечена как простой пользователя (вышел порог idle).</summary>
    public bool IsIdle { get; set; }

    /// <summary>Id категории, сопоставленной правилами (null, если правила не нашли совпадения).</summary>
    public int? CategoryId { get; set; }

    /// <summary>Навигационное свойство категории.</summary>
    public Category? Category { get; set; }

    /// <summary>Удобное локальное время начала (вычисляется на лету, не хранится).</summary>
    public DateTime StartedAtLocal => StartedAtUtc.ToLocalTime();
}
