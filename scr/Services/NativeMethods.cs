using System;
using System.Runtime.InteropServices;

namespace FlibSystem.Services
{
    public static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SHQUERYRBINFO
        {
            public int cbSize;
            public long i64Size;
            public long i64NumItems;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int SHQueryRecycleBin(string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, uint dwFlags);

        public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        public const int TokenUser = 1;

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool GetTokenInformation(IntPtr tokenHandle, int tokenInformationClass,
            IntPtr tokenInformation, uint tokenInformationLength, out uint returnLength);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LookupAccountSid(string lpSystemName, byte[] sid,
            System.Text.StringBuilder lpName, ref uint cchName,
            System.Text.StringBuilder lpReferencedDomainName, ref uint cchReferencedDomainName,
            out int peUse);

        [DllImport("advapi32.dll")]
        public static extern uint GetLengthSid(IntPtr pSid);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        public static bool TryGetAccountSid(IntPtr processHandle, out byte[] sid)
        {
            sid = null;
            IntPtr token;
            if (!OpenProcessToken(processHandle, PROCESS_QUERY_LIMITED_INFORMATION, out token)) return false;
            try
            {
                uint len = 0;
                GetTokenInformation(token, TokenUser, IntPtr.Zero, 0, out len);
                if (len < IntPtr.Size) return false;

                IntPtr buffer = Marshal.AllocHGlobal((int)len);
                try
                {
                    if (!GetTokenInformation(token, TokenUser, buffer, len, out len)) return false;
                    IntPtr sidPtr = Marshal.ReadIntPtr(buffer);
                    if (sidPtr == IntPtr.Zero) return false;

                    uint sidLen = GetLengthSid(sidPtr);
                    if (sidLen == 0 || sidLen > 256) return false;
                    sid = new byte[sidLen];
                    Marshal.Copy(sidPtr, sid, 0, (int)sidLen);
                    return true;
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            finally
            {
                CloseHandle(token);
            }
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessageTimeout(
            IntPtr hWnd, uint Msg, UIntPtr wParam, string lParam,
            uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);

        public const uint WM_SETTINGCHANGE = 0x001A;
        public const uint SMTO_ABORTIFHUNG = 0x0002;

public const uint SHERB_NOCONFIRMATION = 0x00000001;
        public const uint SHERB_NOPROGRESSUI = 0x00000002;
        public const uint SHERB_NOSOUND = 0x00000004;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        public static long GetTotalPhysicalMemory()
        {
            try
            {
                var mem = new MEMORYSTATUSEX();
                mem.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
                return GlobalMemoryStatusEx(ref mem) ? (long)mem.ullTotalPhys : 0;
            }
            catch { return 0; }
        }

        public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

        public const int DWMW_SBT_TRANSIENTWINDOW = 3;
        public const int DWMWCP_ROUND = 2;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        // ==================== GetSystemTimes / GetTickCount64 ====================

        [StructLayout(LayoutKind.Sequential)]
        public struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        [DllImport("kernel32.dll")]
        public static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

        [DllImport("kernel32.dll")]
        public static extern ulong GetTickCount64();

        public static ulong FileTimeToUlong(FILETIME ft)
        {
            return ((ulong)ft.dwHighDateTime << 32) | ft.dwLowDateTime;
        }

        // ==================== SendMessageTimeout WM_NULL ====================

        public const uint WM_NULL = 0x0000;

        public static void ApplyWindowChrome(IntPtr hwnd)
        {
            int acrylic = DWMW_SBT_TRANSIENTWINDOW;
            DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref acrylic, sizeof(int));

            int corners = DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref corners, sizeof(int));

            int dark = 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
        }
    }
}

