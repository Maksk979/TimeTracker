namespace TimeTracker.Core.Configuration;

/// <summary>
/// Настройки трекинга. Хранятся в JSON рядом с приложением и редактируются
/// через окно "Настройки".
/// </summary>
public sealed class TrackerSettings
{
    /// <summary>Интервал опроса активного окна в миллисекундах.</summary>
    public int PollIntervalMs { get; set; } = 1000;

    /// <summary>
    /// Порог простоя в секундах. Если пользователь не трогал мышь/клавиатуру
    /// дольше этого значения, текущая сессия закрывается как IsIdle=true.
    /// </summary>
    public int IdleThresholdSeconds { get; set; } = 60;

    /// <summary>
    /// Минимальная длительность сессии в секундах, чтобы её сохранить.
    /// Короче — отбрасывается (короткие переключения между окнами не интересны).
    /// </summary>
    public int MinSessionSeconds { get; set; } = 3;

    /// <summary>
    /// Если false — заголовки окон не сохраняются (только имя процесса).
    /// Для пользователей, кому важна приватность.
    /// </summary>
    public bool StoreWindowTitles { get; set; } = true;

    /// <summary>Запущен ли трекинг (можно поставить на паузу через трей).</summary>
    public bool IsTrackingEnabled { get; set; } = true;
}
