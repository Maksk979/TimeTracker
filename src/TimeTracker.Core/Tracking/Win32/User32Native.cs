using System.Runtime.InteropServices;
using System.Text;

namespace TimeTracker.Core.Tracking.Win32;

/// <summary>
/// P/Invoke-обёртки над user32.dll для опроса активного окна и детекта простоя.
/// Только Windows — весь код трекинга зависит от этих вызовов.
/// </summary>
internal static class User32Native
{
    private const string User32 = "user32.dll";

    /// <summary>Дескриптор активного (foreground) окна.</summary>
    [DllImport(User32, SetLastError = true)]
    internal static extern IntPtr GetForegroundWindow();

    /// <summary>
    /// Возвращает id потока, владеющего окном, и по выходному параметру — id процесса.
    /// </summary>
    [DllImport(User32, SetLastError = true)]
    internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    /// <summary>
    /// Длина текста окна (в символах) для расчёта размера буфера под GetWindowText.
    /// </summary>
    [DllImport(User32, SetLastError = true)]
    internal static extern int GetWindowTextLength(IntPtr hWnd);

    /// <summary>Читает заголовок окна в StringBuilder.</summary>
    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    /// <summary>
    /// Число миллисекунд с момента старта системы. Используется вместе с
    /// GetLastInputInfo для расчёта времени бездействия пользователя.
    /// </summary>
    [DllImport(User32)]
    internal static extern uint GetTickCount();

    /// <summary>
    /// Заполняет структуру LASTINPUTINFO временем последнего ввода.
    /// </summary>
    [DllImport(User32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [StructLayout(LayoutKind.Sequential)]
    internal struct LASTINPUTINFO
    {
        /// <summary>Размер структуры в байтах — обязательное поле.</summary>
        public uint cbSize;

        /// <summary>TickCount последнего ввода от пользователя.</summary>
        public uint dwTime;
    }
}
