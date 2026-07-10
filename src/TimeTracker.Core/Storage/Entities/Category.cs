namespace TimeTracker.Core.Storage.Entities;

/// <summary>
/// Категория активности: "Разработка", "Браузер", "Общение" и т.д.
/// К сессиям привязывается через правила (CategoryRule).
/// </summary>
public sealed class Category
{
    public int Id { get; set; }

    /// <summary>Название категории для отображения.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>HEX-цвет для графиков, например "#4C9F70".</summary>
    public string ColorHex { get; set; } = "#4C9F70";

    /// <summary>Иконка/эмодзи для UI.</summary>
    public string? Icon { get; set; }

    /// <summary>Правила, относящие окна к этой категории.</summary>
    public List<CategoryRule> Rules { get; set; } = new();

    /// <summary>Сессии, отнесённые к категории.</summary>
    public List<ActivitySession> Sessions { get; set; } = new();
}
