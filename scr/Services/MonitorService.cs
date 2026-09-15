using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace FlibSystem.Services
{
    public class MonitorSample
    {
        public double CpuPercent;
        public double CpuCore0Percent;
        public double[] CpuPerCore;
        public double CpuClockMHz;
        public double CpuBaseMHz;
        public long RamUsedMb;
        public long RamTotalMb;
        public double RamPercent;
        public double NetDownBps;
        public double NetUpBps;
        public double DiskPercent;
        public double GpuPercent;
        public double GpuMemUsedMb;
        public double GpuMemTotalMb;
        public int ProcessCount;
        public double UptimeHours;
    }

    public class MonitorService : IDisposable
    {
        private ulong _prevIdle, _prevKernel, _prevUser;
        private bool _hasPrevCpu;
        private PerformanceCounter _cpuTotal;
        private PerformanceCounter[] _cpuCores;
        private PerformanceCounter _cpuFreq;
        private PerformanceCounter _ramAvailable;
        private PerformanceCounter _diskTime;
        private PerformanceCounter[] _netRxCounters;
        private PerformanceCounter[] _netTxCounters;
        private PerformanceCounter[] _gpuEngineCounters;
        private PerformanceCounter _gpuMemUsed;
        private PerformanceCounter _gpuMemTotal;
        private bool _cpuSupported;
        private bool _gpuSupported;
        private bool _diskSupported;
        private bool _netSupported;
        private bool _ramSupported;
        private long _totalRamMb;
        private DateTime _startTime = DateTime.UtcNow;

        public MonitorService()
        {
            _totalRamMb = NativeMethods.GetTotalPhysicalMemory() / (1024 * 1024);
            InitCpuCounters();
            InitRamCounters();
            InitDiskCounters();
            InitNetCounters();
            InitGpuCounters();
        }

        private void InitCpuCounters()
        {
            try
            {
                _cpuTotal = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                _cpuTotal.NextValue();
                _cpuSupported = true;
            }
            catch { _cpuSupported = false; }

            try
            {
                var cpuFreq = new PerformanceCounter("Processor Information", "% of Maximum Frequency", "_Total", true);
                cpuFreq.NextValue();
                _cpuFreq = cpuFreq;
            }
            catch { _cpuFreq = null; }

            try
            {
                var category = new PerformanceCounterCategory("Processor Information");
                var instances = category.GetInstanceNames()
                    .Where(n => n.StartsWith("0,") || n.StartsWith("1,"))
                    .OrderBy(n => n)
                    .ToArray();
                if (instances.Length == 0)
                {
                    instances = category.GetInstanceNames()
                        .Where(n => n != "_Total" && n != "Utility" && n != "Performance Limit Utilization")
                        .OrderBy(n => n)
                        .ToArray();
                }

                _cpuCores = new PerformanceCounter[instances.Length];
                for (int i = 0; i < instances.Length; i++)
                {
                    try
                    {
                        var c = new PerformanceCounter("Processor Information", "% Processor Utility", instances[i], true);
                        c.NextValue();
                        _cpuCores[i] = c;
                    }
                    catch { _cpuCores = null; break; }
                }
            }
            catch { _cpuCores = null; }
        }

        private void InitRamCounters()
        {
            try
            {
                _ramAvailable = new PerformanceCounter("Memory", "Available MBytes", null, true);
                _ramAvailable.NextValue();
                _ramSupported = true;
            }
            catch { _ramSupported = false; }
        }

        private void InitDiskCounters()
        {
            try
            {
                _diskTime = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total", true);
                _diskTime.NextValue();
                _diskSupported = true;
            }
            catch { _diskSupported = false; }
        }

        private void InitNetCounters()
        {
            try
            {
                var cat = new PerformanceCounterCategory("Network Interface");
                var instances = cat.GetInstanceNames()
                    .Where(n => !n.Contains("Loopback") && !n.Contains("Teredo") && !n.Contains("isatap"))
                    .ToArray();

                if (instances.Length == 0) instances = cat.GetInstanceNames().ToArray();

                _netRxCounters = new PerformanceCounter[instances.Length];
                _netTxCounters = new PerformanceCounter[instances.Length];
                for (int i = 0; i < instances.Length; i++)
                {
                    try
                    {
                        _netRxCounters[i] = new PerformanceCounter("Network Interface", "Bytes Received/sec", instances[i], true);
                        _netRxCounters[i].NextValue();
                        _netTxCounters[i] = new PerformanceCounter("Network Interface", "Bytes Sent/sec", instances[i], true);
                        _netTxCounters[i].NextValue();
                    }
                    catch
                    {
                        _netRxCounters = null;
                        _netTxCounters = null;
                        break;
                    }
                }
                if (_netRxCounters != null && _netRxCounters.Length > 0) _netSupported = true;
            }
            catch { _netSupported = false; }
        }

        private void InitGpuCounters()
        {
            try
            {
                var cat = new PerformanceCounterCategory("GPU Engine");
                var instances = cat.GetInstanceNames()
                    .Where(n => n.Contains("engtype_3D") || n.Contains("engtype_VideoDecode") || n.Contains("engtype_VideoEncode") || n.Contains("engtype_Copy"))
                    .ToArray();

                if (instances.Length == 0) instances = cat.GetInstanceNames().ToArray();

                _gpuEngineCounters = new PerformanceCounter[instances.Length];
                for (int i = 0; i < instances.Length; i++)
                {
                    try
                    {
                        _gpuEngineCounters[i] = new PerformanceCounter("GPU Engine", "Utilization Percentage", instances[i], true);
                        _gpuEngineCounters[i].NextValue();
                    }
                    catch
                    {
                        _gpuEngineCounters = null;
                        break;
                    }
                }
            }
            catch { _gpuEngineCounters = null; }

            try
            {
                var memCat = new PerformanceCounterCategory("GPU Adapter Memory");
                var memInstances = memCat.GetInstanceNames();
                if (memInstances.Length > 0)
                {
                    _gpuMemUsed = new PerformanceCounter("GPU Adapter Memory", "Dedicated Usage", memInstances[0], true);
                    _gpuMemUsed.NextValue();
                    _gpuMemTotal = new PerformanceCounter("GPU Adapter Memory", "Dedicated Limit", memInstances[0], true);
                    _gpuMemTotal.NextValue();
                }
            }
            catch
            {
                _gpuMemUsed = null;
                _gpuMemTotal = null;
            }

            _gpuSupported = (_gpuEngineCounters != null && _gpuEngineCounters.Length > 0);
        }

        public MonitorSample Sample()
        {
            var sample = new MonitorSample();
            sample.ProcessCount = Safe(() => Process.GetProcesses().Length, 0);
            sample.RamTotalMb = _totalRamMb;

            sample.UptimeHours = (DateTime.UtcNow - _startTime).TotalHours;

            // Overall CPU from GetSystemTimes (kernel+idle+user deltas)
            double sysCpu = GetSystemCpuPercent();
            sample.CpuPercent = Math.Min(sysCpu, 100);
            if (sysCpu <= 0 && _cpuSupported)
                sample.CpuPercent = Math.Min(Safe(() => _cpuTotal.NextValue(), 0.0), 100);

            if (_cpuCores != null && _cpuCores.Length > 0)
            {
                sample.CpuPerCore = new double[_cpuCores.Length];
                for (int i = 0; i < _cpuCores.Length; i++)
                {
                    double val = Safe(() => _cpuCores[i].NextValue(), 0.0);
                    sample.CpuPerCore[i] = Math.Min(val, 100);
                    if (i == 0) sample.CpuCore0Percent = val;
                }
            }
            else
            {
                sample.CpuPerCore = new double[0];
            }

            if (_cpuFreq != null)
            {
                double pct = Safe(() => _cpuFreq.NextValue(), 0);
                double baseMHz = Safe(() => {
                    using (var wmi = new System.Management.ManagementObjectSearcher("SELECT MaxClockSpeed FROM Win32_Processor"))
                    {
                        foreach (System.Management.ManagementObject obj in wmi.Get())
                            return Convert.ToDouble(obj["MaxClockSpeed"]);
                    }
                    return 3000.0;
                }, 3000.0);
                sample.CpuBaseMHz = baseMHz;
                sample.CpuClockMHz = baseMHz * Math.Min(pct, 100) / 100.0;
            }

            if (_ramSupported)
            {
                double availMb = Safe(() => _ramAvailable.NextValue(), 0);
                sample.RamUsedMb = _totalRamMb - (long)availMb;
                sample.RamPercent = _totalRamMb > 0 ? (double)sample.RamUsedMb / _totalRamMb * 100 : 0;
            }
            else
            {
                var mem = new NativeMethods.MEMORYSTATUSEX();
                mem.dwLength = (uint)Marshal.SizeOf(typeof(NativeMethods.MEMORYSTATUSEX));
                if (NativeMethods.GlobalMemoryStatusEx(ref mem))
                {
                    sample.RamUsedMb = (long)((mem.ullTotalPhys - mem.ullAvailPhys) / (1024 * 1024));
                    sample.RamPercent = mem.dwMemoryLoad;
                }
            }

            if (_diskSupported)
            {
                sample.DiskPercent = Safe(() => Math.Min(_diskTime.NextValue(), 100), 0);
            }

            if (_netSupported && _netRxCounters != null)
            {
                double rx = 0, tx = 0;
                for (int i = 0; i < _netRxCounters.Length; i++)
                {
                    rx += Safe(() => _netRxCounters[i].NextValue(), 0);
                    tx += Safe(() => _netTxCounters[i].NextValue(), 0);
                }
                sample.NetDownBps = rx;
                sample.NetUpBps = tx;
            }

            if (_gpuSupported && _gpuEngineCounters != null)
            {
                double gpuTotal = 0;
                for (int i = 0; i < _gpuEngineCounters.Length; i++)
                {
                    gpuTotal += Safe(() => _gpuEngineCounters[i].NextValue(), 0);
                }
                sample.GpuPercent = Math.Min(gpuTotal, 100);
            }

            if (_gpuMemUsed != null)
            {
                sample.GpuMemUsedMb = Safe(() => _gpuMemUsed.NextValue() / (1024 * 1024), 0);
                sample.GpuMemTotalMb = Safe(() => _gpuMemTotal.NextValue() / (1024 * 1024), 0);
            }

            return sample;
        }

        private static T Safe<T>(Func<T> f, T fallback)
        {
            try { return f(); }
            catch { return fallback; }
        }

        private double GetSystemCpuPercent()
        {
            NativeMethods.FILETIME idle, kernel, user;
            if (!NativeMethods.GetSystemTimes(out idle, out kernel, out user)) return 0;

            ulong i = NativeMethods.FileTimeToUlong(idle);
            ulong k = NativeMethods.FileTimeToUlong(kernel);
            ulong u = NativeMethods.FileTimeToUlong(user);

            if (!_hasPrevCpu)
            {
                _prevIdle = i; _prevKernel = k; _prevUser = u; _hasPrevCpu = true;
                return 0;
            }

            ulong idleDelta = i - _prevIdle;
            ulong kernelDelta = k - _prevKernel;
            ulong userDelta = u - _prevUser;

            _prevIdle = i; _prevKernel = k; _prevUser = u;

            ulong total = kernelDelta + userDelta;
            if (total == 0) return 0;

            double busy = total - idleDelta;
            return busy / total * 100.0;
        }

        public void Dispose()
        {
            DisposeCounter(_cpuTotal);
            DisposeCounter(_cpuFreq);
            DisposeCounter(_ramAvailable);
            DisposeCounter(_diskTime);
            DisposeCounter(_gpuMemUsed);
            DisposeCounter(_gpuMemTotal);
            if (_cpuCores != null) foreach (var c in _cpuCores) DisposeCounter(c);
            if (_netRxCounters != null) foreach (var c in _netRxCounters) DisposeCounter(c);
            if (_netTxCounters != null) foreach (var c in _netTxCounters) DisposeCounter(c);
            if (_gpuEngineCounters != null) foreach (var c in _gpuEngineCounters) DisposeCounter(c);
        }

        private static void DisposeCounter(PerformanceCounter c)
        {
            if (c != null) try { c.Dispose(); } catch { }
        }
    }
}
