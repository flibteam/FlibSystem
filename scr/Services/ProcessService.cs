using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using FlibSystem.Models;

namespace FlibSystem.Services
{
    public static class ProcessService
    {
        private static readonly HashSet<string> ProtectedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "system", "system idle process", "registry", "memory compression", "secure system",
            "smss", "csrss", "wininit", "winlogon", "services", "lsass", "svchost",
            "dwm", "audiodg", "fontdrvhost", "sihost", "ctfmon", "shellhostbroker",
            "searchhost", "startmenuexperiencehost", "runtimebroker", "shellexperiencehost",
            "textinputhost", "lockapp", "logonui", "dismhost", "taskhostw"
        };

        private static readonly Dictionary<string, BitmapSource> IconCache = new Dictionary<string, BitmapSource>(StringComparer.OrdinalIgnoreCase);
        private static readonly object IconLock = new object();
        private static readonly Dictionary<int, CpuSnapshot> _prevSnap = new Dictionary<int, CpuSnapshot>();

        private struct CpuSnapshot
        {
            public TimeSpan CpuTime;
            public DateTime Timestamp;
        }

        private static T Safe<T>(Func<T> f, T fallback)
        {
            try { return f(); }
            catch { return fallback; }
        }

        public static bool IsProtectedProcess(Process p)
        {
            string name = Safe(() => p.ProcessName, "");
            if (ProtectedNames.Contains(name)) return true;
            try { if (p.Id == Process.GetCurrentProcess().Id) return true; } catch { }

            try
            {
                byte[] sid;
                if (NativeMethods.TryGetAccountSid(p.Handle, out sid))
                {
                    uint nameLen = 0, domainLen = 0;
                    int sidType;
                    if (!NativeMethods.LookupAccountSid(null, sid, null, ref nameLen, null, ref domainLen, out sidType))
                        return false;
                    if (nameLen == 0 || domainLen == 0) return false;

                    var account = new System.Text.StringBuilder((int)nameLen);
                    var domain = new System.Text.StringBuilder((int)domainLen);
                    if (NativeMethods.LookupAccountSid(null, sid, account, ref nameLen, domain, ref domainLen, out sidType))
                    {
                        string acc = account.ToString().ToUpperInvariant();
                        if (acc == "SYSTEM" || acc == "LOCAL SERVICE" || acc == "NETWORK SERVICE")
                            return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private static bool IsWindowHung(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return false;
            try
            {
                UIntPtr result;
                bool ok = NativeMethods.SendMessageTimeout(hwnd, NativeMethods.WM_NULL, UIntPtr.Zero, null,
                    NativeMethods.SMTO_ABORTIFHUNG, 250, out result) != IntPtr.Zero;
                return !ok;
            }
            catch { return false; }
        }

        private static BitmapSource GetIcon(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            lock (IconLock)
            {
                BitmapSource bs;
                if (IconCache.TryGetValue(fileName, out bs)) return bs;
            }
            try
            {
                Icon ico = Icon.ExtractAssociatedIcon(fileName);
                if (ico != null)
                {
                    BitmapSource bs;
                    bs = Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    bs.Freeze();
                    ico.Dispose();
                    lock (IconLock) IconCache[fileName] = bs;
                    return bs;
                }
            }
            catch { }
            return null;
        }

        public static List<ProcessItem> GetLiveSnapshot()
        {
            int cores = Environment.ProcessorCount;
            DateTime now = DateTime.UtcNow;
            long totalBytes = NativeMethods.GetTotalPhysicalMemory();
            long totalMb = totalBytes > 0 ? totalBytes / (1024 * 1024) : 1;
            var newSnap = new Dictionary<int, CpuSnapshot>();
            var list = new List<ProcessItem>();

            foreach (Process p in Process.GetProcesses())
            {
                using (p)
                {
                    int pid = p.Id;
                    ProcessItem item = null;
                    CpuSnapshot snap = new CpuSnapshot { Timestamp = now };
                    try
                    {
                        string name = Safe(() => p.ProcessName, "");
                        string fileName = Safe(() => p.MainModule?.FileName, null);
                        long memMb = Safe(() => p.WorkingSet64 / (1024 * 1024), 0L);
                        int threads = Safe(() => p.Threads.Count, 0);
                        string startTime = Safe(() => p.StartTime.ToString("dd.MM.yyyy HH:mm"), "");
                        bool prot = IsProtectedProcess(p);
                        snap.CpuTime = Safe(() => p.TotalProcessorTime, TimeSpan.Zero);

                        double cpu = 0;
                        CpuSnapshot prev;
                        if (_prevSnap.TryGetValue(pid, out prev))
                        {
                            double elapsed = (now - prev.Timestamp).TotalMilliseconds;
                            double delta = (snap.CpuTime - prev.CpuTime).TotalMilliseconds;
                            if (elapsed > 50 && delta >= 0)
                                cpu = delta / elapsed / cores * 100.0;
                            if (cpu > 100) cpu = 100;
                        }

                        bool hung = prot ? false : IsWindowHung(p.MainWindowHandle);

                        item = new ProcessItem
                        {
                            Id = pid,
                            Name = name,
                            Threads = threads,
                            MemoryMb = memMb,
                            MemoryPercent = (double)memMb / totalMb * 100.0,
                            StartTime = startTime,
                            FileName = fileName,
                            IsProtected = prot,
                            IsHung = hung,
                            CpuPercent = cpu,
                            Icon = GetIcon(fileName)
                        };
                    }
                    catch { }
                    if (item != null)
                    {
                        newSnap[pid] = snap;
                        list.Add(item);
                    }
                }
            }

            _prevSnap.Clear();
            foreach (var kv in newSnap) _prevSnap[kv.Key] = kv.Value;

            return list;
        }

        public static void Kill(ProcessItem item)
        {
            if (item == null || item.IsProtected) return;
            try { using (Process p = Process.GetProcessById(item.Id)) { p.Kill(); } } catch { }
        }

        public static void OpenFileLocation(ProcessItem item)
        {
            if (string.IsNullOrEmpty(item.FileName)) return;
            try { Process.Start("explorer.exe", "/select, \"" + item.FileName + "\""); } catch { }
        }
    }
}
