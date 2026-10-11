using System.Runtime.InteropServices;
using static RegionToShare.NativeMethods;

namespace RegionToShare;

/// <summary>
/// A global low level mouse hook. Events are raised on the thread that installed the hook.
/// </summary>
internal sealed class MouseHook : IDisposable
{
    // Keep a reference to the delegate, else it would be garbage collected while the hook is installed.
    private readonly LowLevelMouseProc _hookProc;
    private IntPtr _hookHandle;

    public MouseHook()
    {
        _hookProc = HookProc;
        _hookHandle = SetWindowsHookEx(WH_MOUSE_LL, _hookProc, GetModuleHandle(null), 0);
    }

    public event Action<POINT>? Moved;

    public event Action<ClickButton, bool>? ButtonChanged;

    public void Dispose()
    {
        if (_hookHandle == IntPtr.Zero)
            return;

        UnhookWindowsHookEx(_hookHandle);
        _hookHandle = IntPtr.Zero;
    }

    private IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            try
            {
                var message = wParam.ToInt32();

                switch (message)
                {
                    case WM_MOUSEMOVE:
                        Moved?.Invoke(Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam).pt);
                        break;
                    case WM_LBUTTONDOWN:
                    case WM_LBUTTONUP:
                        ButtonChanged?.Invoke(ClickButton.Left, message == WM_LBUTTONDOWN);
                        break;
                    case WM_RBUTTONDOWN:
                    case WM_RBUTTONUP:
                        ButtonChanged?.Invoke(ClickButton.Right, message == WM_RBUTTONDOWN);
                        break;
                }
            }
            catch
            {
                // Never let an exception escape into the hook chain.
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }
}

internal enum ClickButton
{
    Left,
    Right
}
