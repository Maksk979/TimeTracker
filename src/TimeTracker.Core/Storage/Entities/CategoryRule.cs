namespace TimeTracker.Core.Storage.Entities;

/// <summary>
/// Способ сопоставления значения окна с шаблоном.
/// Substring — простое вхождение (без учёта регистра).
/// Regex — регулярное выражение целиком.
/// Exact — точное совпадение.
/// </summary>
public enum RuleMatchType
{
    Substring = 0,
    Regex = 1,
    Exact = 2,
}

/// <summary>
/// К какой части информации об окне применяется правило.
/// </summary>
public enum RuleFieldType
{
    ProcessName = 0,
    WindowTitle = 1,
    ExecutableName = 2,
}

/// <summary>
/// Правило: если поле окна (<see cref="FieldType"/>) удовлетворяет
/// шаблону (<see cref="MatchType"/>, <see cref="Pattern"/>),
/// сессия относится к категории-владельцу правила.
/// Правила сортируются по Priority — чем меньше число, тем выше приоритет.
/// </summary>
public sealed class CategoryRule
{
    public int Id { get; set; }

    /// <summary>Id категории, к которой относится совпавшее окно.</summary>
    public int CategoryId { get; set; }

    /// <summary>Навигация к категории.</summary>
    public Category? Category { get; set; }

    /// <summary>К какой части окна применяется.</summary>
    public RuleFieldType FieldType { get; set; } = RuleFieldType.ProcessName;

    /// <summary>Способ сопоставления.</summary>
    public RuleMatchType MatchType { get; set; } = RuleMatchType.Substring;

    /// <summary>Шаблон / значение для сопоставления.</summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Приоритет: меньшее число = выше приоритет.
    /// У первого совпавшего правила выигрывает его категория.
    /// </summary>
    public int Priority { get; set; } = 100;

    /// <summary>Правило включено (можно временно отключать, не удаляя).</summary>
    public bool IsEnabled { get; set; } = true;
}
