using System.Windows.Forms;

namespace RegionToShare
{
    public class RenderTarget : Control
    {
        public override bool PreProcessMessage(ref Message msg)
        {
            switch (msg.Msg)
            {
                case NativeMethods.WM_NCPAINT:
                // case NativeMethods.WM_PAINT:
                case NativeMethods.WM_ERASEBKGND:
                    return true;
            }

            return base.PreProcessMessage(ref msg);
        }
    }
}
