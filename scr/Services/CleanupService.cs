using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlibSystem.Models;

namespace FlibSystem.Services
{
    public static class CleanupService
    {
        private static string LocalAppData
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData); }
        }

        private static string AppData
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); }
        }

        private static string WinDir
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.Windows); }
        }

        private static string UserTemp
        {
            get { return Path.GetTempPath(); }
        }

        public static List<CleanupItem> BuildItems()
        {
            var list = new List<CleanupItem>();

            list.Add(new CleanupItem
            {
                Name = "Временные файлы пользователя",
                Description = "Папка %TEMP% текущего пользователя",
                Kind = "folder",
                GetPath = () => UserTemp
            });

            list.Add(new CleanupItem
            {
                Name = "Временные файлы Windows",
                Description = "C:\\Windows\\Temp",
                Kind = "folder",
                GetPath = () => Path.Combine(WinDir, "Temp")
            });

            list.Add(new CleanupItem
            {
                Name = "Prefetch",
                Description = "Кэш предварительной загрузки приложений",
                Kind = "folder",
                GetPath = () => Path.Combine(WinDir, "Prefetch")
            });

            list.Add(new CleanupItem
            {
                Name = "Служебные файлы Windows Update",
                Description = "C:\\Windows\\SoftwareDistribution\\Download",
                Kind = "folder",
                GetPath = () => Path.Combine(WinDir, "SoftwareDistribution", "Download")
            });

            list.Add(new CleanupItem
            {
                Name = "Миниатюры (thumbcache)",
                Description = "Кэш эскизов изображений в проводнике",
                Kind = "thumbcache",
                GetPath = () => Path.Combine(LocalAppData, "Microsoft", "Windows", "Explorer")
            });

            list.Add(new CleanupItem
            {
                Name = "Недавние документы",
                Description = "Список «Недавние» в проводнике",
                Kind = "folder",
                GetPath = () => Environment.GetFolderPath(Environment.SpecialFolder.Recent)
            });

            list.Add(new CleanupItem
            {
                Name = "Кэш Google Chrome",
                Description = "Кэш браузера Google Chrome",
                Kind = "folder",
                GetPath = () => Path.Combine(LocalAppData, "Google", "Chrome", "User Data", "Default", "Cache")
            });

            list.Add(new CleanupItem
            {
                Name = "Кэш Microsoft Edge",
                Description = "Кэш браузера Microsoft Edge (Chromium)",
                Kind = "folder",
                GetPath = () => Path.Combine(LocalAppData, "Microsoft", "Edge", "User Data", "Default", "Cache")
            });

            list.Add(new CleanupItem
            {
                Name = "Кэш Mozilla Firefox",
                Description = "Кэш браузера Mozilla Firefox",
                Kind = "folder",
                GetPath = () => Path.Combine(LocalAppData, "Mozilla", "Firefox", "Profiles")
            });

            list.Add(new CleanupItem
            {
                Name = "Корзина",
                Description = "Полная очистка корзины",
                Kind = "recycle",
                GetPath = () => ""
            });

            return list;
        }

        public static long GetSize(CleanupItem item)
        {
            if (item.Kind == "recycle") return GetRecycleBinSize();
            string path = item.PathText;
            if (string.IsNullOrEmpty(path)) return 0;

            if (item.Kind == "thumbcache")
            {
                long total = 0;
                try
                {
                    foreach (string f in Directory.GetFiles(path, "thumbcache_*.db", SearchOption.TopDirectoryOnly))
                    {
                        try { total += new FileInfo(f).Length; } catch { }
                    }
                }
                catch { }
                return total;
            }

            if (item.Kind == "folder" && Path.GetFileName(path) == "Profiles")
            {
                long total = 0;
                try
                {
                    foreach (string profile in Directory.GetDirectories(path))
                    {
                        string cache = Path.Combine(profile, "cache2");
                        if (Directory.Exists(cache)) total += GetFolderSize(cache);
                    }
                }
                catch { }
                return total;
            }

            return Directory.Exists(path) ? GetFolderSize(path) : 0;
        }

        public static long GetFolderSize(string path, int depth = 0)
        {
            if (depth > 12) return 0;
            long total = 0;
            try
            {
                foreach (string f in Directory.GetFiles(path))
                {
                    try { total += new FileInfo(f).Length; }
                    catch { }
                }
                foreach (string d in Directory.GetDirectories(path))
                {
                    try { total += GetFolderSize(d, depth + 1); }
                    catch { }
                }
            }
            catch { }
            return total;
        }

        public static long GetRecycleBinSize()
        {
            try
            {
                var info = new NativeMethods.SHQUERYRBINFO();
                info.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(NativeMethods.SHQUERYRBINFO));
                NativeMethods.SHQueryRecycleBin(null, ref info);
                return info.i64Size;
            }
            catch { return 0; }
        }

        public static void Clean(CleanupItem item)
        {
            if (item.Kind == "recycle")
            {
                NativeMethods.SHEmptyRecycleBin(
                    IntPtr.Zero, null,
                    NativeMethods.SHERB_NOCONFIRMATION | NativeMethods.SHERB_NOPROGRESSUI | NativeMethods.SHERB_NOSOUND);
                item.SizeBytes = 0;
                return;
            }

            string path = item.PathText;
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) { item.SizeBytes = 0; return; }

            if (item.Kind == "thumbcache")
            {
                foreach (string f in Directory.GetFiles(path, "thumbcache_*.db", SearchOption.TopDirectoryOnly))
                {
                    try { File.Delete(f); } catch { }
                }
                item.SizeBytes = 0;
                return;
            }

            if (Path.GetFileName(path) == "Profiles")
            {
                foreach (string profile in Directory.GetDirectories(path))
                {
                    string cache = Path.Combine(profile, "cache2");
                    if (Directory.Exists(cache)) DeleteContents(cache);
                }
                item.SizeBytes = 0;
                return;
            }

            DeleteContents(path);
            item.SizeBytes = 0;
        }

        private static void DeleteContents(string path)
        {
            foreach (string f in Directory.GetFiles(path))
            {
                try { File.Delete(f); } catch { }
            }
            foreach (string d in Directory.GetDirectories(path))
            {
                try { Directory.Delete(d, true); } catch { }
            }
        }

        public static void CleanTemp()
        {
            CleanSimple(UserTemp);
            CleanSimple(Path.Combine(WinDir, "Temp"));
        }

        private static void CleanSimple(string path)
        {
            if (!Directory.Exists(path)) return;
            DeleteContents(path);
        }
    }
}

