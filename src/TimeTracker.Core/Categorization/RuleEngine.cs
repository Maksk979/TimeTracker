using System.Text.RegularExpressions;
using TimeTracker.Core.Storage.Entities;

namespace TimeTracker.Core.Categorization;

/// <summary>
/// Информация об окне для сопоставления правилами. Только нужные поля.
/// </summary>
public readonly record struct WindowForCategorization(
    string ProcessName,
    string WindowTitle,
    string? ExecutableName);

/// <summary>
/// Сопоставляет окно с категорией по набору правил.
/// Правила сортируются по Priority; выигрывает первое совпавшее.
/// Чистая логика без БД — легко тестировать.
/// </summary>
public sealed class RuleEngine
{
    /// <summary>
    /// Находит категорию для окна по списку правил.
/// Возвращает null, если ни одно правило не сработало.
    /// </summary>
    public Category? Match(WindowForCategorization window, IEnumerable<Category> categories)
    {
        ArgumentNullException.ThrowIfNull(window.ProcessName);

        // Правила всех категорий, отсортированные по приоритету (меньше = важнее).
        var orderedRules = categories
            .SelectMany(c => c.Rules, (cat, rule) => (cat, rule))
            .Where(x => x.rule.IsEnabled)
            .OrderBy(x => x.rule.Priority)
            .ThenBy(x => x.rule.Id)
            .ToList();

        foreach (var (category, rule) in orderedRules)
        {
            if (Matches(rule, window))
            {
                return category;
            }
        }

        return null;
    }

    private static bool Matches(CategoryRule rule, WindowForCategorization window)
    {
        var fieldValue = rule.FieldType switch
        {
            RuleFieldType.ProcessName => window.ProcessName,
            RuleFieldType.WindowTitle => window.WindowTitle,
            RuleFieldType.ExecutableName => window.ExecutableName ?? string.Empty,
            _ => string.Empty,
        };

        return rule.MatchType switch
        {
            RuleMatchType.Exact => string.Equals(fieldValue, rule.Pattern, StringComparison.OrdinalIgnoreCase),
            RuleMatchType.Substring => fieldValue.Contains(rule.Pattern, StringComparison.OrdinalIgnoreCase),
            RuleMatchType.Regex => TryRegex(fieldValue, rule.Pattern),
            _ => false,
        };
    }

    private static bool TryRegex(string input, string pattern)
    {
        try
        {
            return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            // Некорректный паттерн — правило просто не сработает.
            return false;
        }
    }
}
