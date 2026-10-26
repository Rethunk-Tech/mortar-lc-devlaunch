using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DevLaunch;

internal static class WindowTitle
{
    private delegate bool WindowVisitor(IntPtr window, IntPtr data);

    // Keep a reference so the GC doesn't collect the callback while Windows is still calling it.
    private static readonly WindowVisitor visit = Rename;
    private static string title = "";
    private static uint process;

    public static void Set(string text)
    {
        title = text;
        process = (uint)Process.GetCurrentProcess().Id;
        try
        {
            EnumWindows(visit, IntPtr.Zero);
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            // Not on Windows or Wine. The corner label still works.
        }
    }

    private static bool Rename(IntPtr window, IntPtr data)
    {
        GetWindowThreadProcessId(window, out uint owner);
        if (owner == process && IsWindowVisible(window))
        {
            SetWindowTextW(window, title);
        }
        return true;
    }

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(WindowVisitor visitor, IntPtr data);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool SetWindowTextW(IntPtr window, string text);
}
