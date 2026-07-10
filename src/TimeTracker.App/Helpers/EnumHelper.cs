using TimeTracker.Core.Storage.Entities;

namespace TimeTracker.App.Helpers;

/// <summary>
/// Хелпер для отображения значений enum в XAML (ComboBox и т.д.).
/// </summary>
public static class EnumHelper
{
    public static RuleFieldType[] FieldTypes { get; } = Enum.GetValues<RuleFieldType>();
    public static RuleMatchType[] MatchTypes { get; } = Enum.GetValues<RuleMatchType>();
}
