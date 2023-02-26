using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static RegionToShare.NativeMethods;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Point = System.Drawing.Point;

namespace RegionToShare
{
    internal struct Transformations
    {
        public Matrix ToDevice { get; set; }
        public Matrix FromDevice { get; set; }
    }

    internal static class ExtensionMethods
    {
        public static Thickness Transform(this Matrix matrix, Thickness value)
        {
            var topLeft = matrix.Transform(new Vector(value.Left, value.Top));
            var bottomRight = matrix.Transform(new Vector(value.Right, value.Bottom));

            return new Thickness(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
        }

        public static Transformations GetDeviceTransformations(this HwndTarget? compositionTarget)
        {
            return new()
            {
                FromDevice = compositionTarget?.TransformFromDevice ?? Matrix.Identity,
                ToDevice = compositionTarget?.TransformToDevice ?? Matrix.Identity
            };
        }

        public static WINDOWPLACEMENT GetWindowPlacement(this IntPtr hWnd)
        {
            var value = WINDOWPLACEMENT.Default;
            NativeMethods.GetWindowPlacement(hWnd, ref value);
            return value;
        }

        public static string Serialize(this RECT rect)
        {
            return $"{rect.Left}\t{rect.Top}\t{rect.Right}\t{rect.Bottom}";
        }

        public static void DeserializeFrom(this ref RECT rect, string value)
        {
            try
            {
                var parts = value.Split('\t').Select(int.Parse).ToArray();
                if (parts.Length != 4)
                    return;

                rect.Left = parts[0];
                rect.Top = parts[1];
                rect.Right = Math.Max(rect.Left + 200, parts[2]);
                rect.Bottom = Math.Max(rect.Top + 200, parts[3]);
            }
            catch
            {
                // invalid, just go with input;
            }
        }

        public static void DrawCursor(this Graphics graphics, RECT nativeRect)
        {
            CURSORINFO pci = default;
            pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));

            if (!GetCursorInfo(ref pci) || (pci.flags != CURSOR_SHOWING))
                return;

            var cursor = Cursor.Current;
            if (cursor == null)
                return;

            var location = new Point(
                cursor.HotSpot.X + pci.ptScreenPos.X - nativeRect.Left,
                cursor.HotSpot.Y + pci.ptScreenPos.Y - nativeRect.Top);

            cursor?.Draw(graphics, new Rectangle(location, cursor.Size));
        }

        private const int PatternSize = 128;

        public static Brush GenerateRandomBrush(Color themeColor) =>
            new ImageBrush(GenerateColoredDots(PatternSize, PatternSize, themeColor))
            {
                Opacity = 0.4,
                Viewport = new Rect(0, 0, PatternSize, PatternSize),
                ViewportUnits = BrushMappingMode.Absolute,
                TileMode = TileMode.Tile,
                Stretch = Stretch.None,
            };

        private static BitmapSource GenerateColoredDots(int resX, int resY, Color color)
        {
            var random = new Random(0);

            var bytesPerRow = resX * 3; // Rgb24

            var pixels = new byte[bytesPerRow * resY];

            for (var i = 0; i < bytesPerRow * resY; i += 3)
            {
                var factor = random.NextDouble();
                pixels[i] = (byte)(color.R * factor);
                pixels[i + 1] = (byte)(color.G * factor);
                pixels[i + 2] = (byte)(color.B * factor);
            }

            var wb = new WriteableBitmap(resX, resY, 96.0, 96.0, PixelFormats.Rgb24, null);
            wb.WritePixels(new Int32Rect(0, 0, resX, resY), pixels, bytesPerRow, 0);
            return wb;
        }
    }
}