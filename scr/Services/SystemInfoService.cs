using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management;
using FlibSystem.Models;

namespace FlibSystem.Services
{
    public static class SystemInfoService
    {
        private const string OSKey = @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion";

        private static IEnumerable<ManagementObject> Query(string wql)
        {
            using (var searcher = new ManagementObjectSearcher(wql))
            {
                foreach (ManagementObject o in searcher.Get())
                {
                    yield return o;
                }
            }
        }

        private static string First(ManagementObject o, string property)
        {
            try
            {
                object v = o[property];
                return v == null ? "" : v.ToString().Trim();
            }
            catch { return ""; }
        }

        private static string FormatSize(double bytes)
        {
            double value = bytes;
            string[] units = { "Б", "КБ", "МБ", "ГБ", "ТБ" };
            int i = 0;
            while (value >= 1024 && i < units.Length - 1) { value /= 1024; i++; }
            return value.ToString(i == 0 ? "0" : "0.##") + " " + units[i];
        }

        public static SystemSummary GetSummary()
        {
            var s = new SystemSummary();

            var cpu = Query("SELECT Name FROM Win32_Processor").FirstOrDefault();
            s.Cpu = cpu != null ? First(cpu, "Name") : Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");

var cs = Query("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem").FirstOrDefault();
            double total = 0;
            if (cs != null)
            {
                double.TryParse(First(cs, "TotalPhysicalMemory"), NumberStyles.Any, CultureInfo.InvariantCulture, out total);
            }
            if (total <= 0) total = NativeMethods.GetTotalPhysicalMemory();
            s.Ram = FormatSize(total);

            string productName = RegistryHelper.ReadString(OSKey, "ProductName", "Windows");
            string displayVersion = RegistryHelper.ReadString(OSKey, "DisplayVersion", "");
            string build = RegistryHelper.ReadString(OSKey, "CurrentBuildNumber", "");
            string arch = Environment.Is64BitOperatingSystem ? "64-разрядная" : "32-разрядная";
            s.Os = productName + " " + displayVersion + " (сборка " + build + ", " + arch + ")";

            return s;
        }

        public static List<InfoGroup> Gather()
        {
            var groups = new List<InfoGroup>();

            // ОС
            var osGroup = new InfoGroup { Title = "Операционная система", Glyph = "\uE7FC" };
            string productName = RegistryHelper.ReadString(OSKey, "ProductName", "Windows");
            string edition = RegistryHelper.ReadString(OSKey, "EditionID", "");
            string displayVersion = RegistryHelper.ReadString(OSKey, "DisplayVersion", "");
            string build = RegistryHelper.ReadString(OSKey, "CurrentBuildNumber", "");
            string installDateRaw = RegistryHelper.ReadString(OSKey, "InstallDate", "");

            osGroup.Items.Add(new InfoItem { Label = "Версия", Value = productName });
            if (!string.IsNullOrEmpty(edition)) osGroup.Items.Add(new InfoItem { Label = "Редакция", Value = edition });
            osGroup.Items.Add(new InfoItem { Label = "Сборка", Value = build + (string.IsNullOrEmpty(displayVersion) ? "" : " (" + displayVersion + ")") });
            osGroup.Items.Add(new InfoItem { Label = "Разрядность", Value = Environment.Is64BitOperatingSystem ? "64-разрядная" : "32-разрядная" });

            long installEpoch;
            if (long.TryParse(installDateRaw, out installEpoch) && installEpoch > 0)
            {
                try
                {
                    osGroup.Items.Add(new InfoItem
                    {
                        Label = "Дата установки",
                        Value = DateTimeOffset.FromUnixTimeSeconds(installEpoch).ToLocalTime().ToString("dd.MM.yyyy")
                    });
                }
                catch { }
            }

            var osWmi = Query("SELECT LastBootUpTime FROM Win32_OperatingSystem").FirstOrDefault();
            if (osWmi != null)
            {
                object boot = osWmi["LastBootUpTime"];
                if (boot is DateTime)
                {
                    TimeSpan up = DateTime.Now - (DateTime)boot;
                    osGroup.Items.Add(new InfoItem
                    {
                        Label = "Время работы",
                        Value = (int)up.TotalDays + " дн. " + up.Hours + " ч. " + up.Minutes + " мин."
                    });
                }
            }
            groups.Add(osGroup);

            // Процессор
            var cpuGroup = new InfoGroup { Title = "Процессор", Glyph = "\uE950" };
            foreach (ManagementObject o in Query("SELECT Name, Manufacturer, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed, L2CacheSize, L3CacheSize FROM Win32_Processor"))
            {
                cpuGroup.Items.Add(new InfoItem { Label = "Модель", Value = First(o, "Name") });
                cpuGroup.Items.Add(new InfoItem { Label = "Производитель", Value = First(o, "Manufacturer") });
                cpuGroup.Items.Add(new InfoItem { Label = "Ядра", Value = First(o, "NumberOfCores") });
                cpuGroup.Items.Add(new InfoItem { Label = "Потоки", Value = First(o, "NumberOfLogicalProcessors") });
                string mhz = First(o, "MaxClockSpeed");
                if (!string.IsNullOrEmpty(mhz))
                {
                    double ghz = 0;
                    double.TryParse(mhz, NumberStyles.Any, CultureInfo.InvariantCulture, out ghz);
                    cpuGroup.Items.Add(new InfoItem { Label = "Частота", Value = ghz > 1000 ? (ghz / 1000).ToString("0.00") + " ГГц" : mhz + " МГц" });
                }
                string l2 = First(o, "L2CacheSize");
                string l3 = First(o, "L3CacheSize");
                if (!string.IsNullOrEmpty(l2)) cpuGroup.Items.Add(new InfoItem { Label = "Кэш L2", Value = (long.Parse(l2) / 1024.0).ToString("0.#") + " МБ" });
                if (!string.IsNullOrEmpty(l3)) cpuGroup.Items.Add(new InfoItem { Label = "Кэш L3", Value = (long.Parse(l3) / 1024.0).ToString("0.#") + " МБ" });
                break;
            }
            groups.Add(cpuGroup);

            // Память
            var memGroup = new InfoGroup { Title = "Память", Glyph = "\uE8F1" };
            var cs = Query("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem").FirstOrDefault();
            var osMem = Query("SELECT FreePhysicalMemory, TotalVisibleMemorySize FROM Win32_OperatingSystem").FirstOrDefault();
            if (osMem != null)
            {
                double freeKb, totalKb;
                double.TryParse(First(osMem, "FreePhysicalMemory"), NumberStyles.Any, CultureInfo.InvariantCulture, out freeKb);
                double.TryParse(First(osMem, "TotalVisibleMemorySize"), NumberStyles.Any, CultureInfo.InvariantCulture, out totalKb);
                if (totalKb > 0)
                {
                    memGroup.Items.Add(new InfoItem { Label = "Установлено", Value = FormatSize(totalKb * 1024) });
                    memGroup.Items.Add(new InfoItem { Label = "Свободно", Value = FormatSize(freeKb * 1024) });
                    double usedPct = (totalKb - freeKb) / totalKb * 100;
                    memGroup.Items.Add(new InfoItem { Label = "Используется", Value = usedPct.ToString("0") + " %" });
                }
            }
            groups.Add(memGroup);

            // Видео
            var gpuGroup = new InfoGroup { Title = "Видео", Glyph = "\uE7F4" };
            int gpuCount = 0;
            foreach (ManagementObject o in Query("SELECT Name, AdapterRAM, DriverVersion, VideoModeDescription FROM Win32_VideoController"))
            {
                gpuGroup.Items.Add(new InfoItem { Label = "Видеоадаптер", Value = First(o, "Name") });
                string ram = First(o, "AdapterRAM");
                long ramBytes;
                if (long.TryParse(ram, out ramBytes) && ramBytes > 0)
                {
                    gpuGroup.Items.Add(new InfoItem { Label = "Видеопамять", Value = FormatSize(ramBytes) });
                }
                string dv = First(o, "DriverVersion");
                if (!string.IsNullOrEmpty(dv)) gpuGroup.Items.Add(new InfoItem { Label = "Версия драйвера", Value = dv });
                string vmode = First(o, "VideoModeDescription");
                if (!string.IsNullOrEmpty(vmode)) gpuGroup.Items.Add(new InfoItem { Label = "Разрешение", Value = vmode });
                if (++gpuCount >= 2) break;
            }
            groups.Add(gpuGroup);

            // Материнская плата и BIOS
            var mbGroup = new InfoGroup { Title = "Материнская плата", Glyph = "\uE950" };
            foreach (ManagementObject o in Query("SELECT Manufacturer, Product, Version FROM Win32_BaseBoard"))
            {
                mbGroup.Items.Add(new InfoItem { Label = "Производитель", Value = First(o, "Manufacturer") });
                mbGroup.Items.Add(new InfoItem { Label = "Модель", Value = First(o, "Product") });
                break;
            }
            foreach (ManagementObject o in Query("SELECT Manufacturer, SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS"))
            {
                string man = First(o, "Manufacturer");
                string ver = First(o, "SMBIOSBIOSVersion");
                mbGroup.Items.Add(new InfoItem { Label = "BIOS", Value = (man + " " + ver).Trim() });
                object rd = o["ReleaseDate"];
                if (rd is DateTime)
                {
                    mbGroup.Items.Add(new InfoItem { Label = "Дата BIOS", Value = ((DateTime)rd).ToString("dd.MM.yyyy") });
                }
                break;
            }
            groups.Add(mbGroup);

            // Накопители
            var diskGroup = new InfoGroup { Title = "Накопители", Glyph = "\uE8B9" };
            try
            {
                foreach (var drv in System.IO.DriveInfo.GetDrives())
                {
                    if (drv.IsReady)
                    {
                        string total = drv.TotalSize > 0 ? FormatSize(drv.TotalSize) : "";
                        string free = FormatSize(drv.TotalFreeSpace);
                        string pct = drv.TotalSize > 0
                            ? ((drv.TotalSize - drv.TotalFreeSpace) / (double)drv.TotalSize * 100).ToString("0") + " %"
                            : "";
                        diskGroup.Items.Add(new InfoItem
                        {
                            Label = drv.Name + " " + drv.DriveFormat,
                            Value = (total + " (занято " + pct + ", свободно " + free + ")").Trim()
                        });
                    }
                }
            }
            catch { }
            groups.Add(diskGroup);

            // Сеть
            var netGroup = new InfoGroup { Title = "Сеть", Glyph = "\uE968" };
            foreach (ManagementObject o in Query("SELECT Name, MACAddress, Speed FROM Win32_NetworkAdapter WHERE PhysicalAdapter = TRUE AND NetEnabled = TRUE"))
            {
                string speedRaw = First(o, "Speed");
                string speed = "";
                long speedBits;
                if (long.TryParse(speedRaw, out speedBits) && speedBits > 0)
                {
                    speed = " (" + (speedBits / 1000000.0).ToString("0.##") + " Мбит/с)";
                }
                netGroup.Items.Add(new InfoItem { Label = "Адаптер", Value = First(o, "Name") + speed });
                string mac = First(o, "MACAddress");
                if (!string.IsNullOrEmpty(mac)) netGroup.Items.Add(new InfoItem { Label = "MAC", Value = mac });
            }
            if (netGroup.Items.Count > 0) groups.Add(netGroup);

            return groups;
        }
    }
}

