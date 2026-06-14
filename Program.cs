using System;
using System.Windows.Forms;

namespace TrayDisplay;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        NativeMethods.SetProcessDpiAwarenessContext(new IntPtr(-4));
        Application.Run(new TrayApp());
    }
}
