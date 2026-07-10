# TimeTracker

Тайм-трекер для Windows. Автоматически отслеживает активные приложения, категоризирует время и показывает отчёты.

## Возможности

- Автоматическое отслеживание активного окна (1 раз в секунду)
- Детект простоя пользователя
- Автокатегоризация по правилам (процесс, заголовок, имя exe)
- Круговая и столбчатая графики за день
- Редактор категорий и правил
- Отчёты за произвольный период с графиками
- Иконка в системном трее с индикацией записи/паузы
- Single-instance (только один экземпляр приложения)
- Тёмная тема (Linear-style)

## Стек

- .NET 10, WPF, C#
- EF Core + SQLite
- LiveCharts2 (графики)
- CommunityToolkit.Mvvm (MVVM)
- Serilog (логи)
- xUnit (тесты)

## Сборка и запуск

### Из исходников
```bash
dotnet run --project src/TimeTracker.App
```

### В исполняемый файл
```bash
# Или двойной клик по build.bat
dotnet publish src/TimeTracker.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
```

Результат: `publish\TimeTracker.exe` (~191 МБ, не требует установленного .NET)

## Структура

```
src/
  TimeTracker.Core/           Логика: трекинг, правила, БД, настройки
    Tracking/                 Win32 P/Invoke, ActivityTracker
    Categorization/           RuleEngine, DefaultRules
    Storage/                  EF DbContext, репозиторий, миграции
    Configuration/            TrackerSettings, SettingsStore
  TimeTracker.App/            WPF: трей, вкладки, ViewModels
    Views/                    TodayView, ChartsView, RulesView, SettingsView, ReportsView
    ViewModels/               TodayViewModel, ChartsViewModel, RulesViewModel, SettingsViewModel, ReportsViewModel
    Tray/                     TrayIconManager (System.Windows.Forms.NotifyIcon)
tests/
  TimeTracker.Core.Tests/     xUnit тесты
```

## Данные

БД и настройки хранятся в `%LocalAppData%\TimeTracker\`:
- `timetracker.db` — SQLite база данных
- `settings.json` — настройки приложения
- `logs/` — логи (ротация по дням, хранение 7 дней)

## Как проверить работу

1. Запустите приложение
2. Подождите 30-60 секунд, переключайтесь между окнами
3. Нажмите "Обновить" на вкладке "Сегодня" — сессии появятся в таблице
4. Проверьте логи: `%LocalAppData%\TimeTracker\logs\`
5. Или откройте БД через DB Browser for SQLite: таблица `sessions`
