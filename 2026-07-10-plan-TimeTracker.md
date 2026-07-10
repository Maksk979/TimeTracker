Plan
Тайм-трекер для Windows на C# (.NET 8 + WPF)
Создаём greenfield-проект в новом локальном репозитории на рабочем столе. После создания проект полностью открывается в Visual Studio Community 2022.

Расположение
Рабочий стол: C:\Users\Honor\Desktop\ (если занято системным OneDrive — проверю и использую C:\Users\Honor\OneDrive\Desktop\)
Папка проекта: Desktop\TimeTracker\
Git-репозиторий инициализируется локально (git init, без remote — удалённый добавим позже, когда захочешь)
Стек
.NET 8 (LTS), WPF, C#
EF Core + Microsoft.Data.Sqlite
H.NotifyIcon.Wpf (трей), LiveCharts2 (графики), CommunityToolkit.Mvvm (MVVM), Serilog (логи), xUnit (тесты)
Структура решения
src/TimeTracker.Core/ — вся логика: Tracking (Win32 P/Invoke, WindowMonitor, ActivityTracker), Categorization (RuleEngine, DefaultRules), Storнаage (EF DbContext, репозиторий), Configuration
src/TimeTracker.App/ — WPF: трей, ViewModels (Today/Reports/Rules/Settings), Views
tests/TimeTracker.Core.Tests/ — xUnit
Первый шаг реализации (сейчас)
Определить путь к рабочему столу (обычный vs OneDrive)
git init в Desktop\TimeTracker\, .gitignore для .NET/WPF
Создать решение и 3 проекта через dotnet CLI
Добавить NuGet-пакеты и ссылки между проектами
Собрать — убедиться, что пустой каркас компилируется
Реализовывать по этапам плана (Win32 → трекинг → БД → категории → трей → MVVM → графики → отчёты)
Ключевые решения
Опрос активного окна в фоновом потоке (1 Гц) с CancellationToken
Сессии накапливаются в памяти и сбрасываются в БД при смене окна/простое — минимум I/O
Детект простоя через GetLastInputInfo
Single-instance через Mutex
Приватность заголовков окон — опция в настройках
Этапы (10 дней)
Каркас решения и проектов — пустое окно собирается
Win32-обёртки + WindowMonitor — лог активного окна
Детект простоя + ActivityTracker — сессии считаются
EF Core: схема (ActivitySession, Category, CategoryRule, AppEntry) + миграции + репозиторий
RuleEngine + DefaultRules
Трей, single-instance
MVVM + вид «Сегодня»
LiveCharts2: круговая по категориям + столбчатая по часам
Редактор правил/категорий + настройки
Отчёты, индикатор записи, иконка, README, упаковка
Тестами
RuleEngine, логика простоя, расчёт длительности сессий.

Вне MVP
Автозапуск с Windows, экспорт CSV, облако, ML-категоризация.

При одобрении — начинаю с проверки окружения и создания репозитория на рабочем столе. Часть проета ты уже сделал, и он находится в заданной директории, найдешь в плане. Определи место на котором остановилась разработка и продолжи выполнение до конца