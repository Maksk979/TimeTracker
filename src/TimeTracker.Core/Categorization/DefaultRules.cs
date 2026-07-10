using TimeTracker.Core.Storage.Entities;

namespace TimeTracker.Core.Categorization;

/// <summary>
/// Стартовый набор категорий и правил для нового пользователя.
/// Создаётся при первом запуске (пустая БД). Пользователь может править их в UI.
/// </summary>
public static class DefaultRules
{
    /// <summary>Возвращает категории с предустановленными правилами и цветами.</summary>
    public static List<Category> Build()
    {
        return
        [
            Category("Разработка", "#4C9F70", "💻", priority: 10, new[]
            {
                Rule(RuleFieldType.ProcessName, "devenv"),        // Visual Studio
                Rule(RuleFieldType.ProcessName, "ServiceHub"),    // VS-компоненты
                Rule(RuleFieldType.ProcessName, "Code"),          // VS Code
                Rule(RuleFieldType.ProcessName, "rider"),         // JetBrains Rider
                Rule(RuleFieldType.ExecutableName, "dotnet.exe"),
                Rule(RuleFieldType.WindowTitle, "ZCode"),
                Rule(RuleFieldType.WindowTitle, "Visual Studio"),
            }),

            Category("Браузер", "#3B82F6", "🌐", priority: 20, new[]
            {
                Rule(RuleFieldType.ProcessName, "chrome"),
                Rule(RuleFieldType.ProcessName, "msedge"),
                Rule(RuleFieldType.ProcessName, "firefox"),
                Rule(RuleFieldType.ProcessName, "brave"),
                Rule(RuleFieldType.ProcessName, "opera"),
            }),

            Category("Общение", "#A855F7", "💬", priority: 30, new[]
            {
                Rule(RuleFieldType.ProcessName, "Telegram"),
                Rule(RuleFieldType.ProcessName, "Discord"),
                Rule(RuleFieldType.ProcessName, "Slack"),
                Rule(RuleFieldType.ProcessName, "vk"),
                Rule(RuleFieldType.ProcessName, "Skype"),
            }),

            Category("Игры", "#EF4444", "🎮", priority: 40, new[]
            {
                Rule(RuleFieldType.ProcessName, "Steam"),
                Rule(RuleFieldType.ProcessName, "EpicGamesLauncher"),
                Rule(RuleFieldType.WindowTitle, "Steam", RuleMatchType.Substring),
            }),

            Category("Офис и документы", "#F59E0B", "📄", priority: 50, new[]
            {
                Rule(RuleFieldType.ProcessName, "WINWORD"),
                Rule(RuleFieldType.ProcessName, "EXCEL"),
                Rule(RuleFieldType.ProcessName, "POWERPNT"),
                Rule(RuleFieldType.ProcessName, "ONENOTE"),
                Rule(RuleFieldType.ProcessName, "Acrobat"),
                Rule(RuleFieldType.ProcessName, "SumatraPDF"),
            }),

            Category("Терминал", "#6B7280", "⌨️", priority: 15, new[]
            {
                Rule(RuleFieldType.ProcessName, "WindowsTerminal"),
                Rule(RuleFieldType.ProcessName, "cmd"),
                Rule(RuleFieldType.ProcessName, "powershell"),
                Rule(RuleFieldType.ProcessName, "pwsh"),
                Rule(RuleFieldType.ProcessName, "GitBash"),
            }),
        ];
    }

    private static Category Category(
        string name, string color, string icon,
        int priority, IEnumerable<CategoryRule> rules)
    {
        var cat = new Category { Name = name, ColorHex = color, Icon = icon };
        var ruleList = rules.ToList();
        ruleList.ForEach(r =>
        {
            r.Category = cat;
            r.Priority = priority;
        });
        cat.Rules = ruleList;
        return cat;
    }

    private static CategoryRule Rule(
        RuleFieldType field, string pattern, RuleMatchType match = RuleMatchType.Substring)
        => new()
        {
            FieldType = field,
            Pattern = pattern,
            MatchType = match,
            IsEnabled = true,
        };
}
