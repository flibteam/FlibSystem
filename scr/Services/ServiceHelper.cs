using System;
using System.ServiceProcess;

namespace FlibSystem.Services
{
    public static class ServiceHelper
    {
        private static string ServiceKey(string name)
        {
            return @"HKLM\SYSTEM\CurrentControlSet\Services\" + name;
        }

        public static bool IsDisabled(string name)
        {
            try
            {
                using (var sc = new ServiceController(name))
                {
                    return sc.StartType == ServiceStartMode.Disabled;
                }
            }
            catch { return false; }
        }

        public static bool IsStopped(string name)
        {
            try
            {
                using (var sc = new ServiceController(name))
                {
                    return sc.Status == ServiceControllerStatus.Stopped;
                }
            }
            catch { return true; }
        }

        public static void SetStartValue(string name, int value)
        {
            try { RegistryHelper.WriteInt(ServiceKey(name), "Start", value); }
            catch { }
        }

        public static void Stop(string name)
        {
            try
            {
                using (var sc = new ServiceController(name))
                {
                    if (sc.Status != ServiceControllerStatus.Stopped)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(8));
                    }
                }
            }
            catch { }
        }

        public static void Start(string name)
        {
            try
            {
                using (var sc = new ServiceController(name))
                {
                    if (sc.Status == ServiceControllerStatus.Stopped && sc.StartType != ServiceStartMode.Disabled)
                    {
                        sc.Start();
                    }
                }
            }
            catch { }
        }

        public static void SetDisabled(string name)
        {
            SetStartValue(name, 4);
            Stop(name);
        }

        public static void SetAuto(string name)
        {
            SetStartValue(name, 2);
            Start(name);
        }
    }
}

