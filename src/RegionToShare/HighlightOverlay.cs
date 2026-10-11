using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using static RegionToShare.NativeMethods;

namespace RegionToShare;

/// <summary>
/// A click through, top most layered window that shows the highlight ring around the mouse cursor on the screen.
/// </summary>
/// <remarks>
/// The overlay is excluded from screen capture, so the ring in the shared image is controlled independently.
/// </remarks>
internal sealed class HighlightOverlay : NativeWindow, IDisposable
{
    private const int SW_SHOWNOACTIVATE = 4;

    private int _size;

    public HighlightOverlay()
    {
        CreateHandle(new CreateParams
        {
            Caption = "Region to Share - Mouse Highlight",
            Style = WS_POPUP,
            ExStyle = WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TOPMOST
        });

        // Requires Windows 10 2004 or later; on older versions the ring on the screen also shows up in the shared image.
        SetWindowDisplayAffinity(Handle, WDA_EXCLUDEFROMCAPTURE);
    }

    /// <summary>
    /// Redraws the overlay, centered at the given position.
    /// </summary>
    public void Render(POINT center, int size, Action<Graphics, float, float> draw)
    {
        _size = Math.Max(1, size);

        using var bitmap = new Bitmap(_size, _size, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            draw(graphics, _size / 2f, _size / 2f);
        }

        var screenDc = GetDC(IntPtr.Zero);
        var memoryDc = CreateCompatibleDC(screenDc);
        var bitmapHandle = bitmap.GetHbitmap(Color.FromArgb(0));
        var oldBitmap = SelectObject(memoryDc, bitmapHandle);

        try
        {
            var position = TopLeft(center);
            var bitmapSize = new SIZE(_size, _size);
            var source = new POINT();
            var blend = new BLENDFUNCTION { BlendOp = AC_SRC_OVER, SourceConstantAlpha = 255, AlphaFormat = AC_SRC_ALPHA };

            UpdateLayeredWindow(Handle, screenDc, ref position, ref bitmapSize, memoryDc, ref source, 0, ref blend, ULW_ALPHA);
        }
        finally
        {
            SelectObject(memoryDc, oldBitmap);
            DeleteObject(bitmapHandle);
            DeleteDC(memoryDc);
            ReleaseDC(IntPtr.Zero, screenDc);
        }

        ShowWindow(Handle, SW_SHOWNOACTIVATE);
    }

    public void MoveTo(POINT center)
    {
        var position = TopLeft(center);

        SetWindowPos(Handle, HWND_TOPMOST, position.X, position.Y, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE);
    }

    public void Dispose()
    {
        DestroyHandle();
    }

    private POINT TopLeft(POINT center) => new(center.X - _size / 2, center.Y - _size / 2);
}
