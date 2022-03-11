using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

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

        public static NativeMethods.WINDOWPLACEMENT GetWindowPlacement(this IntPtr hWnd)
        {
            var value = NativeMethods.WINDOWPLACEMENT.Default;
            NativeMethods.GetWindowPlacement(hWnd, ref value);
            return value;
        }

        public static string Serialize(this NativeMethods.RECT rect)
        {
            return $"{rect.Left}\t{rect.Top}\t{rect.Right}\t{rect.Bottom}";
        }

        public static void DeserializeFrom(this ref NativeMethods.RECT rect, string value)
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
    }
}
