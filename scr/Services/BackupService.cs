using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlibSystem.Models;

namespace FlibSystem.Services
{
    public static class BackupService
    {
        public static string BaseDir
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "FlibSystemBackups");
            }
        }

        public static string CreateBackup()
        {
            string dir = Path.Combine(BaseDir, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            Directory.CreateDirectory(dir);

            string[] paths = TweakCatalog.RegistryPaths;
            foreach (string p in paths)
            {
                Export(p, dir);
            }

            File.WriteAllLines(Path.Combine(dir, "_registry.txt"), paths);
            File.WriteAllText(Path.Combine(dir, "_info.txt"),
                "FlibSystem — резервная копия реестра\nСоздано: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
            return dir;
        }

        private static string Sanitize(string regPath)
        {
            return regPath.Replace('\\', '_').Replace(' ', '_');
        }

        private static void Export(string regPath, string dir)
        {
            string file = Path.Combine(dir, Sanitize(regPath) + ".reg");
            string args = "export \"" + regPath + "\" \"" + file + "\" /y";
            ProcessHelper.Run("reg.exe", args);
        }

        public static List<BackupEntry> GetBackups()
        {
            var list = new List<BackupEntry>();
            if (!Directory.Exists(BaseDir)) return list;

            foreach (string d in Directory.EnumerateDirectories(BaseDir).OrderByDescending(x => x))
            {
                int count = 0;
                try { count = Directory.GetFiles(d, "*.reg").Length; }
                catch { }

                list.Add(new BackupEntry
                {
                    Folder = d,
                    Created = Directory.GetLastWriteTime(d).ToString("dd.MM.yyyy HH:mm"),
                    FileCount = count.ToString()
                });
            }
            return list;
        }

        public static void Restore(string folder)
        {
            foreach (string f in Directory.GetFiles(folder, "*.reg"))
            {
                string args = "import \"" + f + "\"";
                ProcessHelper.Run("reg.exe", args);
            }
        }

        public static void OpenFolder()
        {
            if (!Directory.Exists(BaseDir)) Directory.CreateDirectory(BaseDir);
            System.Diagnostics.Process.Start("explorer.exe", "\"" + BaseDir + "\"");
        }
    }
}

