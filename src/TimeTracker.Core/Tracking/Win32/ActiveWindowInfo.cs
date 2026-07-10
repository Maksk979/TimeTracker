using System.Diagnostics;

namespace TimeTracker.Core.Tracking.Win32;

/// <summary>
/// Снимок информации об активном окне в один момент времени.
/// Значения могут быть пустыми, если получить их не удалось
/// (например, процесс уже завершился к моменту опроса).
/// </summary>
public sealed record ActiveWindowInfo
{
    /// <summary>Дескриптор окна (HWND). 0, если активного окна нет.</summary>
    public IntPtr WindowHandle { get; init; }

    /// <summary>Имя процесса без расширения, например "devenv" или "chrome".</summary>
    public string ProcessName { get; init; } = string.Empty;

    /// <summary>Имя исполняемого файла, например "devenv.exe".</summary>
    public string? ExecutableName { get; init; }

    /// <summary>Заголовок активного окна.</summary>
    public string WindowTitle { get; init; } = string.Empty;

    public override string ToString() =>
        WindowHandle == IntPtr.Zero ? "(нет активного окна)" : $"{ProcessName} :: {WindowTitle}";
}

/// <summary>
/// Чтение состояния активного окна и времени простоя через Win32.
/// Вся "виндовая" специфика изолирована здесь — остальной код работает с абстракциями.
/// </summary>
public static class ActiveWindowReader
{
    /// <summary>
    /// Возвращает информацию о текущем активном окне.
    /// Возвращает пустой снимок (WindowHandle == Zero), если окно получить не удалось.
    /// </summary>
    public static ActiveWindowInfo GetCurrent()
    {
        var hwnd = User32Native.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return new ActiveWindowInfo { WindowHandle = IntPtr.Zero };
        }

        var title = ReadWindowTitle(hwnd);
        var (processName, executable) = ReadProcessInfo(hwnd);

        return new ActiveWindowInfo
        {
            WindowHandle = hwnd,
            ProcessName = processName,
            ExecutableName = executable,
            WindowTitle = title,
        };
    }

    /// <summary>
    /// Сколько секунд прошло с последнего ввода пользователя (мышь/клавиатура).
    /// Используется для детекта простоя — если значение превышает порог,
    /// текущая сессия закрывается как "неактивная".
    /// </summary>
    public static uint GetIdleSeconds()
    {
        var info = new User32Native.LASTINPUTINFO
        {
            cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<User32Native.LASTINPUTINFO>(),
        };

        if (!User32Native.GetLastInputInfo(ref info))
        {
            return 0;
        }

        // TickCount обнуляется примерно раз в 49 дней — учитываем переполнение.
        var nowTicks = User32Native.GetTickCount();
        var elapsed = nowTicks - info.dwTime;
        return elapsed / 1000u;
    }

    private static string ReadWindowTitle(IntPtr hwnd)
    {
        var length = User32Native.GetWindowTextLength(hwnd);
        if (length <= 0)
        {
            return string.Empty;
        }

        var sb = new System.Text.StringBuilder(length + 1);
        User32Native.GetWindowText(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private static (string processName, string? executable) ReadProcessInfo(IntPtr hwnd)
    {
        try
        {
            User32Native.GetWindowThreadProcessId(hwnd, out var pid);
            if (pid == 0)
            {
                return (string.Empty, null);
            }

            // Процесс мог уже завершиться — обрабатываем спокойно.
            using var proc = Process.GetProcessById((int)pid);
            var name = proc.ProcessName;
            string? exe = null;
            try
            {
                // MainModule может бросать доступ к 64-битным процессам из 32-битных
                // или при недостатке прав — не критично для трекинга.
                exe = proc.MainModule?.ModuleName;
            }
            catch
            {
                // Доступ к MainModule ограничен — оставляем только ProcessName.
            }

            return (name, exe);
        }
        catch (ArgumentException)
        {
            // Процесс не существует — GetProcessById бросает ArgumentException.
            return (string.Empty, null);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Нет прав на чтение процесса — оставляем пустое имя.
            return (string.Empty, null);
        }
    }
}
