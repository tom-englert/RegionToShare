using System.Diagnostics;
using System.Text;
using static RegionToShare.NativeMethods;

namespace RegionToShare;

internal static class WindowFitter
{
    private static readonly HashSet<string> ShellWindowClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd"
    };

    /// <summary>
    /// Moves and resizes the foreground window, so its visible bounds match the region.
    /// Does nothing if the foreground window is not suitable.
    /// </summary>
    public static void FitForegroundWindow(RECT region)
    {
        var windowHandle = GetForegroundWindow();

        if (!IsSuitable(windowHandle))
            return;

        if (IsIconic(windowHandle) || IsZoomed(windowHandle))
        {
            ShowWindow(windowHandle, SW_RESTORE);
        }

        // Set it twice: if the first call has moved the window to a screen with different DPI settings,
        // the window may have resized itself and the invisible frame thickness may have changed.
        for (var i = 0; i < 2; i++)
        {
            var rect = region + DwmGetExtendedFrameBounds(windowHandle);
            SetWindowPos(windowHandle, IntPtr.Zero, rect.Left, rect.Top, rect.Width, rect.Height, SWP_NOACTIVATE | SWP_NOZORDER);
        }
    }

    private static bool IsSuitable(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero || !IsWindowVisible(windowHandle))
            return false;

        if (windowHandle == GetShellWindow())
            return false;

        GetWindowThreadProcessId(windowHandle, out var processId);
        if (processId == Process.GetCurrentProcess().Id)
            return false;

        var className = new StringBuilder(256);
        GetClassName(windowHandle, className, className.Capacity);
        if (ShellWindowClasses.Contains(className.ToString()))
            return false;

        // Popup windows without caption or sizing border, e.g. borderless full screen apps.
        var style = GetWindowLong(windowHandle, GWL_STYLE);
        return (style & (WS_CAPTION | WS_THICKFRAME)) != 0;
    }
}
