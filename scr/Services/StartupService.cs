using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using FlibSystem.Models;

namespace FlibSystem.Services
{
    public static class StartupService
    {
        private const string RunPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ApprovedPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

        public static string StartupFolder
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Microsoft\Windows\Start Menu\Programs\Startup");
            }
        }

        private static string DisabledSubfolder
        {
            get { return Path.Combine(StartupFolder, "_disabled"); }
        }

        public static List<StartupItem> GetItems()
        {
            var list = new List<StartupItem>();
            list.AddRange(ReadRegistryItems(Registry.CurrentUser, "HKCU"));
            list.AddRange(ReadRegistryItems(Registry.LocalMachine, "HKLM"));
            list.AddRange(ReadFolderItems());
            return list;
        }

        private static List<StartupItem> ReadRegistryItems(RegistryKey hive, string hiveName)
        {
            var result = new List<StartupItem>();
            try
            {
                using (RegistryKey runKey = hive.OpenSubKey(RunPath))
                {
                    if (runKey == null) return result;
                    foreach (string name in runKey.GetValueNames())
                    {
                        try
                        {
                            if (!(runKey.GetValue(name) is string cmd)) continue;
                            var item = new StartupItem
                            {
                                Name = name,
                                Command = cmd,
                                Source = hiveName,
                                IsFromFolder = false,
                                FilePath = ExtractFilePath(cmd)
                            };
                            item.CommitEnabled = (it, enabled) => SetRegistryEnabled(hiveName, it.Name, enabled);
                            item.SetInitialState(IsRegistryEnabled(hiveName, name));
                            result.Add(item);
                        }
                        catch { }
                    }
                }
            }
            catch { }
            return result;
        }

        private static List<StartupItem> ReadFolderItems()
        {
            var result = new List<StartupItem>();
            try
            {
                if (Directory.Exists(StartupFolder))
                {
                    foreach (string f in Directory.GetFiles(StartupFolder))
                    {
                        result.Add(CreateFolderItem(f, true));
                    }
                }
                if (Directory.Exists(DisabledSubfolder))
                {
                    foreach (string f in Directory.GetFiles(DisabledSubfolder))
                    {
                        result.Add(CreateFolderItem(f, false));
                    }
                }
            }
            catch { }
            return result;
        }

        private static StartupItem CreateFolderItem(string path, bool enabled)
        {
            var item = new StartupItem
            {
                Name = Path.GetFileName(path),
                Command = path,
                Source = "Папка",
                IsFromFolder = true,
                FilePath = path,
                RelativePath = path
            };
            item.CommitEnabled = ToggleFolderItem;
            item.SetInitialState(enabled);
            return item;
        }

        private static bool IsRegistryEnabled(string hiveName, string name)
        {
            try
            {
                RegistryKey hive = hiveName == "HKLM" ? Registry.LocalMachine : Registry.CurrentUser;
                using (RegistryKey k = hive.OpenSubKey(ApprovedPath))
                {
                    byte[] b = k?.GetValue(name) as byte[];
                    if (b == null || b.Length == 0) return true;
                    byte state = b[0];
                    return state != 0x03 && state != 0x06 && state != 0x07;
                }
            }
            catch { return true; }
        }

        private static bool SetRegistryEnabled(string hiveName, string name, bool enabled)
        {
            try
            {
                RegistryKey hive = hiveName == "HKLM" ? Registry.LocalMachine : Registry.CurrentUser;
                using (RegistryKey k = hive.CreateSubKey(ApprovedPath))
                {
                    byte[] value = new byte[12];
                    value[0] = (byte)(enabled ? 0x02 : 0x03);
                    byte[] ft = BitConverter.GetBytes(DateTime.UtcNow.ToFileTime());
                    Array.Copy(ft, 0, value, 4, 8);
                    k.SetValue(name, value, RegistryValueKind.Binary);
                }
                return true;
            }
            catch { return false; }
        }

        private static bool ToggleFolderItem(StartupItem item, bool enabled)
        {
            try
            {
                if (item == null || string.IsNullOrEmpty(item.RelativePath)) return false;
                string current = item.RelativePath;
                if (!File.Exists(current)) return false;

                if (enabled)
                {
                    if (current.StartsWith(DisabledSubfolder, StringComparison.OrdinalIgnoreCase))
                    {
                        string target = Path.Combine(StartupFolder, Path.GetFileName(current));
                        if (File.Exists(target)) File.Delete(target);
                        File.Move(current, target);
                        item.RelativePath = target;
                        item.FilePath = target;
                    }
                }
                else
                {
                    bool inRoot = current.StartsWith(StartupFolder, StringComparison.OrdinalIgnoreCase) &&
                                  !current.StartsWith(DisabledSubfolder, StringComparison.OrdinalIgnoreCase);
                    if (inRoot)
                    {
                        Directory.CreateDirectory(DisabledSubfolder);
                        string target = Path.Combine(DisabledSubfolder, Path.GetFileName(current));
                        if (File.Exists(target)) File.Delete(target);
                        File.Move(current, target);
                        item.RelativePath = target;
                        item.FilePath = target;
                    }
                }
                return true;
            }
            catch { return false; }
        }

        public static bool Remove(StartupItem item)
        {
            if (item == null) return false;
            try
            {
                if (item.IsFromFolder)
                {
                    if (!string.IsNullOrEmpty(item.RelativePath) && File.Exists(item.RelativePath))
                    {
                        string trash = Path.Combine(DisabledSubfolder, "removed");
                        Directory.CreateDirectory(trash);
                        string target = Path.Combine(trash, Path.GetFileName(item.RelativePath) + "_" + DateTime.Now.ToString("HHmmss"));
                        File.Move(item.RelativePath, target);
                    }
                    return true;
                }

                RegistryKey hive = item.Source == "HKLM" ? Registry.LocalMachine : Registry.CurrentUser;
                using (RegistryKey k = hive.OpenSubKey(RunPath, true))
                {
                    k?.DeleteValue(item.Name, false);
                }
                using (RegistryKey a = hive.OpenSubKey(ApprovedPath, true))
                {
                    a?.DeleteValue(item.Name, false);
                }
                return true;
            }
            catch { return false; }
        }

        public static void Reveal(StartupItem item)
        {
            if (item == null) return;
            string target = item.IsFromFolder ? item.RelativePath : item.FilePath;
            if (string.IsNullOrEmpty(target)) return;
            try { Process.Start("explorer.exe", "/select, \"" + target + "\""); } catch { }
        }

        public static void OpenStartupFolder()
        {
            try
            {
                Directory.CreateDirectory(StartupFolder);
                Process.Start("explorer.exe", "\"" + StartupFolder + "\"");
            }
            catch { }
        }

        private static string ExtractFilePath(string command)
        {
            if (string.IsNullOrEmpty(command)) return "";
            string t = command.Trim();
            if (t.StartsWith("\""))
            {
                int end = t.IndexOf('"', 1);
                if (end > 0) return t.Substring(1, end - 1);
            }
            int sp = t.IndexOf(' ');
            return sp > 0 ? t.Substring(0, sp) : t;
        }
    }
}