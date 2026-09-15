using System;
using Microsoft.Win32;

namespace FlibSystem.Services
{
    public static class RegistryHelper
    {
        private static RegistryKey GetHive(string root)
        {
            switch (root.ToUpperInvariant())
            {
                case "HKCR":
                case "HKEY_CLASSES_ROOT":
                    return Registry.ClassesRoot;
                case "HKCU":
                case "HKEY_CURRENT_USER":
                    return Registry.CurrentUser;
                case "HKLM":
                case "HKEY_LOCAL_MACHINE":
                    return Registry.LocalMachine;
                case "HKU":
                case "HKEY_USERS":
                    return Registry.Users;
                case "HKCC":
                case "HKEY_CURRENT_CONFIG":
                    return Registry.CurrentConfig;
                default:
                    return Registry.CurrentUser;
            }
        }

        private static void SplitPath(string path, out RegistryKey hive, out string subPath)
        {
            int idx = path.IndexOf('\\');
            string root = idx >= 0 ? path.Substring(0, idx) : path;
            subPath = idx >= 0 ? path.Substring(idx + 1) : "";
            hive = GetHive(root);
        }

        public static RegistryKey Open(string path, bool writable)
        {
            RegistryKey hive;
            string subPath;
            SplitPath(path, out hive, out subPath);
            try
            {
                return string.IsNullOrEmpty(subPath)
                    ? hive
                    : hive.OpenSubKey(subPath, writable);
            }
            catch
            {
                return null;
            }
        }

        public static void EnsureKey(string path)
        {
            RegistryKey hive;
            string subPath;
            SplitPath(path, out hive, out subPath);
            if (string.IsNullOrEmpty(subPath)) return;
            using (RegistryKey key = hive.CreateSubKey(subPath))
            {
            }
        }

        public static void DeleteKey(string path)
        {
            RegistryKey hive;
            string subPath;
            SplitPath(path, out hive, out subPath);
            if (string.IsNullOrEmpty(subPath)) return;
            int last = subPath.LastIndexOf('\\');
            if (last < 0)
            {
                try { hive.DeleteSubKeyTree(subPath, false); }
                catch { }
                return;
            }
            string parent = subPath.Substring(0, last);
            string leaf = subPath.Substring(last + 1);
            try
            {
                using (RegistryKey pk = hive.OpenSubKey(parent, true))
                {
                    pk?.DeleteSubKeyTree(leaf, false);
                }
            }
            catch { }
        }

        public static bool HasValue(string path, string name)
        {
            try
            {
                using (RegistryKey key = Open(path, false))
                {
                    return key?.GetValue(name) != null;
                }
            }
            catch { return false; }
        }

        public static int ReadInt(string path, string name, int defaultValue = 0)
        {
            try
            {
                using (RegistryKey key = Open(path, false))
                {
                    object o = key?.GetValue(name);
                    if (o is int) return (int)o;
                    if (o is long) return (int)(long)o;
                    if (o is byte[] b && b.Length >= 4) return BitConverter.ToInt32(b, 0);
                    return defaultValue;
                }
            }
            catch { return defaultValue; }
        }

        public static string ReadString(string path, string name, string defaultValue = null)
        {
            try
            {
                using (RegistryKey key = Open(path, false))
                {
                    object o = key?.GetValue(name);
                    return o as string ?? defaultValue;
                }
            }
            catch { return defaultValue; }
        }

        public static void WriteInt(string path, string name, int value)
        {
            using (RegistryKey key = Open(path, true))
            {
                if (key == null) throw new InvalidOperationException("Не удалось открыть ключ реестра: " + path);
                key.SetValue(name, value, RegistryValueKind.DWord);
            }
        }

        public static void WriteString(string path, string name, string value)
        {
            using (RegistryKey key = Open(path, true))
            {
                if (key == null) throw new InvalidOperationException("Не удалось открыть ключ реестра: " + path);
                key.SetValue(name, value, RegistryValueKind.String);
            }
        }

        public static void DeleteValue(string path, string name)
        {
            try
            {
                using (RegistryKey key = Open(path, true))
                {
                    key?.DeleteValue(name, false);
                }
            }
            catch { }
        }
    }
}

