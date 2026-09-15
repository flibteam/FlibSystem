using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FlibSystem.Services
{
    public static class ProcessHelper
    {
        public static string Run(string fileName, string arguments, bool waitForExit = true, int timeoutMs = 30000)
        {
            try
            {
                var psi = new ProcessStartInfo(fileName, arguments)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using (Process p = Process.Start(psi))
                {
                    Task<string> tOut = p.StandardOutput.ReadToEndAsync();
                    Task<string> tErr = p.StandardError.ReadToEndAsync();

                    bool finished = p.WaitForExit(timeoutMs);
                    if (!finished)
                    {
                        try { p.Kill(); } catch { }
                        Task.WaitAll(tOut, tErr);
                        return "";
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

