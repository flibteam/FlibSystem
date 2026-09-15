using System;
using System.Collections.Generic;
using System.Linq;
using FlibSystem.Models;

namespace FlibSystem.Services
{
    public static class UwpService
    {
        public static readonly string[] BloatwarePrefixes =
        {
            "Microsoft.Bing", "Microsoft.Xbox", "Microsoft.ZuneMusic", "Microsoft.ZuneVideo",
            "Microsoft.MicrosoftSolitaireCollection", "Microsoft.549981C3F5F10", "Microsoft.People",
            "Microsoft.MixedReality", "Microsoft.GetHelp", "Microsoft.Getstarted", "Microsoft.Todos",
            "Microsoft.Wallet", "Microsoft.OfficeHub", "Microsoft.MicrosoftOfficeHub", "Microsoft.OneConnect",
            "Microsoft.YourPhone", "Microsoft.WindowsFeedbackHub", "Microsoft.MSPaint", "Microsoft.Paint",
            "Microsoft.WindowsMaps", "Microsoft.MicrosoftStickyNotes", "Microsoft.SkypeApp", "Microsoft.Teams",
            "Microsoft.Advertising", "Microsoft.Copilot", "Microsoft.Windows.Photos",
            "Spotify", "Disney.37853FC22B2CE", "Facebook", "Instagram", "TikTok",
            "King.CandyCrush", "king.com", "DolbyLaboratories", "AdobeSystemsIncorporated.AdobePhotoshopExpress",
            "PandoraMedia", "Duolingo", "LinkedIn.LinkedIn"
        };

        public static bool IsBloatware(UwpApp app)
        {
            if (app == null || string.IsNullOrEmpty(app.Name)) return false;
            return BloatwarePrefixes.Any(p =>
                app.Name.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }

        public static List<UwpApp> GetApps()
        {
            string script =
                "Get-AppxPackage -ErrorAction SilentlyContinue | Sort-Object Name | ForEach-Object { " +
                "[string]::Join('|', @($_.Name, $_.Version, $_.PackageFullName, $_.Publisher, $_.InstallLocation)) }";

            string output = RunPowershell(script);
            var list = new List<UwpApp>();

            foreach (string rawLine in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string line = rawLine.Trim();
                string[] parts = line.Split('|');
                if (parts.Length < 4) continue;

                var app = new UwpApp
                {
                    Name = parts[0].Trim(),
                    Version = parts[1].Trim(),
                    PackageFullName = parts[2].Trim(),
                    Publisher = parts[3].Trim(),
                    InstallLocation = parts.Length > 4 ? parts[4].Trim() : ""
                };
                list.Add(app);
            }
            return list;
        }

        public static string Uninstall(UwpApp app)
        {
            string script = "Get-AppxPackage -Name '" + app.Name.Replace("'", "''") + "' | Remove-AppxPackage";
            return RunPowershell(script);
        }

        public static HashSet<string> GetInstalledNames()
        {
            string script =
                "Get-AppxPackage -ErrorAction SilentlyContinue | ForEach-Object { $_.Name }";

            string output = RunPowershell(script);
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string name = line.Trim();
                if (name.Length > 0) names.Add(name);
            }
            return names;
        }

        private static string RunPowershell(string script)
        {
            script = "[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; " +
                     "$OutputEncoding = [System.Text.Encoding]::UTF8; " +
                     script;
            script = script.Replace("\"", "\\\"");
            string args = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"" + script + "\"";

            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo("powershell.exe", args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                using (var p = System.Diagnostics.Process.Start(psi))
                {
                    System.Threading.Tasks.Task<string> tOut = p.StandardOutput.ReadToEndAsync();
                    System.Threading.Tasks.Task<string> tErr = p.StandardError.ReadToEndAsync();
                    if (!p.WaitForExit(120000))
                    {
                        try { p.Kill(); } catch { }
                    }
                    string stdout = tOut.Result;
                    string stderr = tErr.Result;
                    return stdout + (string.IsNullOrEmpty(stderr) ? "" : "\n" + stderr);
                }
            }
            catch
            {
                return "";
            }
        }
    }
}

