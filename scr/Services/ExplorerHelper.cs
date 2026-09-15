using System;
using System.Diagnostics;
using System.Threading;

namespace FlibSystem.Services
{
    public static class ExplorerHelper
    {
        public static void RefreshSettings()
        {
            UIntPtr result;
            NativeMethods.SendMessageTimeout(
                new IntPtr(0xFFFF), NativeMethods.WM_SETTINGCHANGE, UIntPtr.Zero, "Environment",
                NativeMethods.SMTO_ABORTIFHUNG, 5000, out result);
        }

        public static void RestartExplorer()
        {
            foreach (Process p in Process.GetProcessesByName("explorer"))
            {
                try { p.Kill(); } catch { }
            }
            Thread.Sleep(800);
            try { Process.Start("explorer.exe"); } catch { }
        }
    }
}

