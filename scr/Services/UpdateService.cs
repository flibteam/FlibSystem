using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using FlibSystem.Models;

namespace FlibSystem.Services
{
    public static class UpdateService
    {
        public const string RepoUrl = "https://github.com/flibteam/FlibSystem";
        private const string ApiUrl = "https://api.github.com/repos/flibteam/FlibSystem/releases/latest";
        private const string AssetMatch = "FlibSystem.exe";
        private const string UserAgent = "FlibSystem-Updater";
        private const string SkipRegKey = @"Software\FlibSystem";

        public static Version CurrentVersion
        {
            get
            {
                var v = Assembly.GetExecutingAssembly().GetName().Version;
                return v ?? new Version(0, 0, 0);
            }
        }

        public static string CurrentVersionText => CurrentVersion.ToString(3);

        public static async Task<ReleaseInfo> GetLatestReleaseAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var client = new WebClient())
                    {
                        client.Encoding = Encoding.UTF8;
                        client.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                        client.Headers[HttpRequestHeader.Accept] = "application/vnd.github.v3+json";
                        return ParseRelease(client.DownloadString(ApiUrl));
                    }
                }
                catch
                {
                    return null;
                }
            });
        }

        public static bool IsNewerAvailable(ReleaseInfo release)
        {
            if (release == null || string.IsNullOrEmpty(release.TagName)) return false;
            Version latest;
            if (!TryParseVersion(release.TagName, out latest)) return false;
            return latest > CurrentVersion;
        }

        public static bool IsCurrentNewer(ReleaseInfo release)
        {
            if (release == null || string.IsNullOrEmpty(release.TagName)) return false;
            Version latest;
            if (!TryParseVersion(release.TagName, out latest)) return false;
            return latest > new Version(0, 0, 0) && CurrentVersion > latest;
        }

        public static bool TryParseVersion(string tag, out Version version)
        {
            version = null;
            if (string.IsNullOrEmpty(tag)) return false;
            string v = tag.Trim().TrimStart('v', 'V');
            int dash = v.IndexOf('-');
            if (dash > 0) v = v.Substring(0, dash);
            return Version.TryParse(v, out version);
        }

        public static string GetDownloadsPath(ReleaseInfo release)
        {
            string name = string.IsNullOrEmpty(release.AssetName) ? AssetMatch : release.AssetName;
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                name);
        }

        public static void DownloadRelease(ReleaseInfo release, string targetPath,
            Action<long, long> onProgress, Action<string> onCompleted)
        {
            try
            {
                string dir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                var client = new WebClient();
                client.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                client.DownloadProgressChanged += (s, e) =>
                    onProgress?.Invoke(e.BytesReceived, e.TotalBytesToReceive);
                client.DownloadFileCompleted += (s, e) =>
                {
                    client.Dispose();
                    if (e.Error != null)
                        onCompleted?.Invoke(null);
                    else if (e.Cancelled)
                        onCompleted?.Invoke(null);
                    else
                        onCompleted?.Invoke(targetPath);
                };
                client.DownloadFileAsync(new Uri(release.AssetUrl), targetPath);
            }
            catch
            {
                onCompleted?.Invoke(null);
            }
        }

        public static void OpenDownloadsFolder()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");
            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe", "\"" + path + "\"") { UseShellExecute = true });
            }
            catch
            {
            }
        }

        public static string VerifyDownload(string path, ReleaseInfo release)
        {
            try
            {
                string expected = DownloadExpectedHash(release);
                if (string.IsNullOrEmpty(expected)) return null;

                string actual = ComputeSha256(path);
                if (string.IsNullOrEmpty(actual)) return "Не удалось вычислить хэш скачанного файла.";

                if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
                {
                    try { if (File.Exists(path)) File.Delete(path); } catch { }
                    return "Хэш-сумма не совпадает: файл повреждён или модифицирован. " +
                           "Скачивание отменено, файл удалён.";
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static string DownloadExpectedHash(ReleaseInfo release)
        {
            if (release == null || string.IsNullOrEmpty(release.HashUrl)) return null;
            try
            {
                using (var client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                    string content = client.DownloadString(release.HashUrl);
                    if (string.IsNullOrEmpty(content)) return null;

                    var match = System.Text.RegularExpressions.Regex.Match(content,
                        @"(?i)\b[0-9a-f]{64}\b");
                    return match.Success ? match.Value : null;
                }
            }
            catch
            {
                return null;
            }
        }

        public static string ComputeSha256(string path)
        {
            try
            {
                using (var fs = File.OpenRead(path))
                using (var sha = new System.Security.Cryptography.SHA256Managed())
                {
                    byte[] hash = sha.ComputeHash(fs);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            catch
            {
                return null;
            }
        }

        public static string GetSkippedVersion()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(SkipRegKey))
                {
                    return key?.GetValue("SkippedUpdate") as string;
                }
            }
            catch
            {
                return null;
            }
        }

        public static void SetSkippedVersion(string version)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(SkipRegKey))
                {
                    key?.SetValue("SkippedUpdate", version ?? string.Empty);
                }
            }
            catch
            {
            }
        }

        private static ReleaseInfo ParseRelease(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            var serializer = new JavaScriptSerializer();
            var root = serializer.Deserialize<Dictionary<string, object>>(json);
            if (root == null) return null;

            var info = new ReleaseInfo
            {
                TagName = GetString(root, "tag_name"),
                Name = GetString(root, "name"),
                Body = GetString(root, "body")
            };

            string published = GetString(root, "published_at");
            if (!string.IsNullOrEmpty(published))
            {
                DateTime dt;
                if (DateTime.TryParse(published, out dt)) info.PublishedAt = dt.ToLocalTime();
            }

            if (root.TryGetValue("assets", out object assetsObj) && assetsObj is object[] assets)
            {
                string exeName = null;
                string exeUrl = null;
                long exeSize = 0;
                string hashName = null;
                string hashUrl = null;

                foreach (var item in assets)
                {
                    if (!(item is Dictionary<string, object> asset)) continue;
                    string name = GetString(asset, "name");
                    if (string.IsNullOrEmpty(name)) continue;

                    if (name.Equals(AssetMatch, StringComparison.OrdinalIgnoreCase))
                    {
                        exeName = name;
                        exeUrl = GetString(asset, "browser_download_url");
                        long.TryParse(GetString(asset, "size"), out exeSize);
                    }
                    else if (name.EndsWith(AssetMatch + ".sha256", StringComparison.OrdinalIgnoreCase))
                    {
                        hashName = name;
                        hashUrl = GetString(asset, "browser_download_url");
                    }
                }

                info.AssetName = exeName;
                info.AssetUrl = exeUrl;
                info.AssetSize = exeSize;
                info.HasAsset = !string.IsNullOrEmpty(exeUrl);
                info.HashFileName = hashName;
                info.HashUrl = hashUrl;
                info.HasHashFile = !string.IsNullOrEmpty(hashUrl);
            }

            return info;
        }

        private static string GetString(Dictionary<string, object> dict, string key)
        {
            if (dict != null && dict.TryGetValue(key, out object value) && value != null)
                return Convert.ToString(value);
            return null;
        }
    }
}