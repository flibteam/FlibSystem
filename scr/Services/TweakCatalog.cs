using System;
using System.Collections.Generic;
using System.Linq;
using FlibSystem.Models;

namespace FlibSystem.Services
{
    public static class TweakCatalog
    {
        private const string ExpAdv = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
        private const string Themes = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string DataCollection = @"HKLM\Software\Policies\Microsoft\Windows\DataCollection";
        private const string Consent = @"HKCU\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore";
        private const string SearchPol = @"HKLM\Software\Policies\Microsoft\Windows\Windows Search";
        private const string SearchHku = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Search";
        private const string SysPol = @"HKLM\Software\Policies\Microsoft\Windows\System";
        private const string PrivacyHku = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Privacy";
        private const string ContentDelivery = @"HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";
        private const string AdvInfo = @"HKCU\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo";
        private const string Wer = @"HKCU\Software\Microsoft\Windows\Windows Error Reporting";
        private const string DesktopIcons = @"HKLM\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel";
        private const string WindowMetrics = @"HKCU\Control Panel\Desktop\WindowMetrics";
        private const string ControlDesktop = @"HKCU\Control Panel\Desktop";
        private const string GameDvr = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\GameDVR";
        private const string GameConfig = @"HKCU\System\GameConfigStore";
        private const string Serialize = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize";
        private const string Power = @"HKLM\SYSTEM\CurrentControlSet\Control\Power";
        private const string Mouse = @"HKCU\Control Panel\Mouse";
        private const string WuAu = @"HKLM\Software\Policies\Microsoft\Windows\WindowsUpdate\AU";
        private const string Crash = @"HKLM\SYSTEM\CurrentControlSet\Control\CrashControl";
        private const string PolSystem = @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";
        private const string Tcpip6 = @"HKLM\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters";
        private const string ContextMenu = @"HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32";
        private const string HighPerfGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";

        private const string CatPersonalization = "Персонализация";
        private const string CatPerformance = "Производительность";
        private const string CatPrivacy = "Конфиденциальность";
        private const string CatTweaks = "Твики системы";

        private static List<Tweak> _all;

        public static IReadOnlyList<Tweak> All
        {
            get { return _all ?? (_all = BuildAll()); }
        }

        public static IReadOnlyList<Tweak> Personalization => All.Where(t => t.Category == CatPersonalization).ToArray();
        public static IReadOnlyList<Tweak> Performance => All.Where(t => t.Category == CatPerformance).ToArray();
        public static IReadOnlyList<Tweak> Privacy => All.Where(t => t.Category == CatPrivacy).ToArray();
        public static IReadOnlyList<Tweak> Tweaks => All.Where(t => t.Category == CatTweaks).ToArray();

        public static string[] RegistryPaths
        {
            get
            {
                return All
                    .SelectMany(t => t.RegistryPaths)
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Distinct()
                    .ToArray();
            }
        }

        private static Tweak Reg(string id, string name, string desc, string glyph, string category,
            string[] paths, Func<bool> get, Action<bool> set, bool warn = false)
        {
            return new Tweak
            {
                Id = id,
                Name = name,
                Description = desc,
                Glyph = glyph,
                Category = category,
                RegistryPaths = paths,
                Getter = get,
                Setter = set,
                HasWarning = warn
            };
        }

        private static Tweak DesktopIcon(string id, string name, string valueName)
        {
            return Reg(
                id, name, "Показывать значок на рабочем столе", "\uE8B7", CatPersonalization,
                new[] { DesktopIcons },
                () => !RegistryHelper.HasValue(DesktopIcons, valueName) ||
                      RegistryHelper.ReadInt(DesktopIcons, valueName, 1) == 0,
                v => RegistryHelper.WriteInt(DesktopIcons, valueName, v ? 0 : 1));
        }

        private static List<Tweak> BuildAll()
        {
            var list = new List<Tweak>();

            // ================== Персонализация ==================
            list.Add(Reg("show_ext", "Показывать расширения файлов",
                "Отображение расширений у файлов в проводнике.",
                "\uE8B7", CatPersonalization, new[] { ExpAdv },
                () => RegistryHelper.ReadInt(ExpAdv, "HideFileExt", 1) == 0,
                v => RegistryHelper.WriteInt(ExpAdv, "HideFileExt", v ? 0 : 1)));

            list.Add(Reg("hidden_files", "Показывать скрытые файлы и папки",
                "Отображение скрытых элементов в проводнике.",
                "\uE8B7", CatPersonalization, new[] { ExpAdv },
                () => RegistryHelper.ReadInt(ExpAdv, "Hidden", 0) == 1,
                v => RegistryHelper.WriteInt(ExpAdv, "Hidden", v ? 1 : 0)));

            list.Add(Reg("full_path_title", "Полный путь в заголовке окна",
                "Отображение полного пути к папке в заголовке окна проводника.",
                "\uE8B7", CatPersonalization, new[] { ExpAdv },
                () => RegistryHelper.ReadInt(ExpAdv, "FullPath", 0) == 1,
                v => RegistryHelper.WriteInt(ExpAdv, "FullPath", v ? 1 : 0)));

            list.Add(Reg("launch_this_pc", "Открывать «Этот компьютер»",
                "Проводник будет открываться сразу на странице «Этот компьютер».",
                "\uE946", CatPersonalization, new[] { ExpAdv },
                () => RegistryHelper.ReadInt(ExpAdv, "LaunchTo", 0) == 1,
                v => RegistryHelper.WriteInt(ExpAdv, "LaunchTo", v ? 1 : 0)));

            list.Add(Reg("dark_apps", "Тёмная тема приложений",
                "Тёмное оформление системных и UWP-приложений.",
                "\uE790", CatPersonalization, new[] { Themes },
                () => RegistryHelper.ReadInt(Themes, "AppsUseLightTheme", 1) == 0,
                v => RegistryHelper.WriteInt(Themes, "AppsUseLightTheme", v ? 0 : 1)));

            list.Add(Reg("dark_system", "Тёмная системная тема",
                "Тёмное оформление системных областей (Панель задач, меню Пуск).",
                "\uE790", CatPersonalization, new[] { Themes },
                () => RegistryHelper.ReadInt(Themes, "SystemUsesLightTheme", 1) == 0,
                v => RegistryHelper.WriteInt(Themes, "SystemUsesLightTheme", v ? 0 : 1)));

            list.Add(Reg("transparency", "Эффекты прозрачности",
                "Включение прозрачности меню Пуск, панели задач и окон.",
                "\uE790", CatPersonalization, new[] { Themes },
                () => RegistryHelper.ReadInt(Themes, "EnableTransparency", 1) == 1,
                v => RegistryHelper.WriteInt(Themes, "EnableTransparency", v ? 1 : 0)));

            list.Add(Reg("taskbar_left", "Панель задач слева",
                "Выравнивание значков панели задач слева (Windows 11).",
                "\uE7F4", CatPersonalization, new[] { ExpAdv },
                () => RegistryHelper.ReadInt(ExpAdv, "TaskbarAl", 1) == 0,
                v => RegistryHelper.WriteInt(ExpAdv, "TaskbarAl", v ? 0 : 1)));

            list.Add(Reg("clock_seconds", "Секунды в часах",
                "Отображение секунд в часах панели задач.",
                "\uE916", CatPersonalization, new[] { ExpAdv },
                () => RegistryHelper.ReadInt(ExpAdv, "ShowSecondsInSystemClock", 0) == 1,
                v => RegistryHelper.WriteInt(ExpAdv, "ShowSecondsInSystemClock", v ? 1 : 0)));

            list.Add(Reg("taskbar_search", "Иконка поиска на панели задач",
                "Показывать иконку поиска вместо скрытого поля.",
                "\uE721", CatPersonalization, new[] { ExpAdv },
                () => RegistryHelper.ReadInt(ExpAdv, "TaskbarSearchIcons", 0) == 1,
                v => RegistryHelper.WriteInt(ExpAdv, "TaskbarSearchIcons", v ? 1 : 0)));

            list.Add(Reg("sync_notifications", "Отключить уведомления OneDrive",
                "Скрыть значок синхронизации OneDrive в проводнике.",
                "\uE74D", CatPersonalization, new[] { ExpAdv },
                () => RegistryHelper.ReadInt(ExpAdv, "ShowSyncProviderNotifications", 1) == 0,
                v => RegistryHelper.WriteInt(ExpAdv, "ShowSyncProviderNotifications", v ? 0 : 1)));

            list.Add(DesktopIcon("icon_thispc", "Значок «Этот компьютер»", "{20D04FE0-3AEA-1069-A2D8-08002B30309D}"));
            list.Add(DesktopIcon("icon_recycle", "Значок «Корзина»", "{645FF040-5081-101B-9F08-00AA002F954E}"));
            list.Add(DesktopIcon("icon_network", "Значок «Сеть»", "{F02C1A0D-2BE1-4350-AA30-5F6C44B7E2B7}"));

            // ================== Производительность ==================
            list.Add(Reg("best_perf", "Максимальная производительность",
                "Настройка визуальных эффектов Windows на максимальную производительность.",
                "\uE945", CatPerformance, new[] { ExpAdv, WindowMetrics, ControlDesktop },
                () => RegistryHelper.ReadInt(ExpAdv, "VisualFXSetting", 1) == 2,
                v =>
                {
                    RegistryHelper.WriteInt(ExpAdv, "VisualFXSetting", v ? 2 : 1);
                    RegistryHelper.WriteInt(WindowMetrics, "MinAnimate", v ? 0 : 1);
                    RegistryHelper.WriteInt(ControlDesktop, "MenuShowDelay", v ? 0 : 400);
                }));

            list.Add(Reg("no_animations", "Отключить анимации",
                "Отключение анимаций сворачивания и разворачивания окон.",
                "\uE945", CatPerformance, new[] { WindowMetrics },
                () => RegistryHelper.ReadInt(WindowMetrics, "MinAnimate", 1) == 0,
                v => RegistryHelper.WriteInt(WindowMetrics, "MinAnimate", v ? 0 : 1)));

            list.Add(Reg("menu_delay", "Убрать задержку меню",
                "Мгновенное открытие контекстных меню.",
                "\uE945", CatPerformance, new[] { ControlDesktop },
                () => RegistryHelper.ReadInt(ControlDesktop, "MenuShowDelay", 400) == 0,
                v => RegistryHelper.WriteInt(ControlDesktop, "MenuShowDelay", v ? 0 : 400)));

            list.Add(Reg("gamedvr", "Отключить GameDVR",
                "Отключение записи игры и фонового захвата Game DVR.",
                "\uE7FC", CatPerformance, new[] { GameDvr, GameConfig },
                () => RegistryHelper.ReadInt(GameDvr, "AllowGameDVR", 1) == 0 &&
                      RegistryHelper.ReadInt(GameConfig, "GameDVR_Enabled", 1) == 0,
                v =>
                {
                    RegistryHelper.WriteInt(GameDvr, "AllowGameDVR", v ? 0 : 1);
                    RegistryHelper.WriteInt(GameConfig, "GameDVR_Enabled", v ? 0 : 1);
                }));

            list.Add(Reg("startup_delay", "Отключить задержку автозагрузки",
                "Убирает задержку запуска программ из автозагрузки после входа.",
                "\uE945", CatPerformance, new[] { Serialize },
                () => RegistryHelper.ReadInt(Serialize, "StartupDelayInMSec", 200) == 0,
                v => RegistryHelper.WriteInt(Serialize, "StartupDelayInMSec", v ? 0 : 200)));

            list.Add(Reg("high_perf_plan", "Электропитание: максимальная производительность",
                "Активация схемы электропитания «Максимальная производительность».",
                "\uE7FC", CatPerformance, new[] { Power },
                () => IsHighPerformanceActive(),
                v =>
                {
                    if (v)
                    {
                        ProcessHelper.Run("powercfg.exe", "/duplicatescheme " + HighPerfGuid, false, 15000);
                        ProcessHelper.Run("powercfg.exe", "/setactive " + HighPerfGuid, false, 15000);
                    }
                    else
                    {
                        ProcessHelper.Run("powercfg.exe", "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e", false, 15000);
                    }
                }));

            list.Add(Reg("hibernate_off", "Отключить гибернацию",
                "Отключает файл гибернации hiberfil.sys, освобождая место на диске.",
                "\uE7FC", CatPerformance, new[] { Power },
                () => RegistryHelper.ReadInt(Power, "HibernateEnabled", 1) == 0,
                v =>
                {
                    ProcessHelper.Run("powercfg.exe", v ? "/h off" : "/h on", false, 15000);
                    RegistryHelper.WriteInt(Power, "HibernateEnabled", v ? 0 : 1);
                }));

            list.Add(Reg("mouse_precision", "Отключить ускорение мыши",
                "Отключает улучшенную точность указателя мыши.",
                "\uE945", CatPerformance, new[] { Mouse },
                () => RegistryHelper.ReadInt(Mouse, "MouseSpeed", 1) == 0,
                v => RegistryHelper.WriteInt(Mouse, "MouseSpeed", v ? 0 : 1)));

            // ================== Конфиденциальность ==================
            list.Add(Reg("telemetry", "Отключить телеметрию",
                "Отключает сбор диагностических данных и службы DiagTrack / dmwappushservice.",
                "\uE72E", CatPrivacy,
                new[] { DataCollection,
                        @"HKLM\SYSTEM\CurrentControlSet\Services\DiagTrack",
                        @"HKLM\SYSTEM\CurrentControlSet\Services\dmwappushservice" },
                () => ServiceHelper.IsDisabled("DiagTrack"),
                v =>
                {
                    if (v)
                    {
                        RegistryHelper.EnsureKey(DataCollection);
                        RegistryHelper.WriteInt(DataCollection, "AllowTelemetry", 0);
                        RegistryHelper.WriteInt(DataCollection, "DisableEnterpriseAuthProxy", 0);
                        ServiceHelper.SetDisabled("DiagTrack");
                        ServiceHelper.SetDisabled("dmwappushservice");
                    }
                    else
                    {
                        RegistryHelper.WriteInt(DataCollection, "AllowTelemetry", 1);
                        ServiceHelper.SetAuto("DiagTrack");
                        ServiceHelper.SetAuto("dmwappushservice");
                    }
                }));

            list.Add(Reg("advertising_id", "Отключить рекламный ID",
                "Отключает рекламный идентификатор устройства.",
                "\uE72E", CatPrivacy, new[] { AdvInfo, PrivacyHku },
                () => RegistryHelper.ReadInt(AdvInfo, "Enabled", 1) == 0,
                v =>
                {
                    RegistryHelper.WriteInt(AdvInfo, "Enabled", v ? 0 : 1);
                    RegistryHelper.WriteInt(PrivacyHku, "AdvertisingEnabled", v ? 0 : 1);
                }));

            list.Add(Reg("tailored_ads", "Отключить персональную рекламу",
                "Windows не будет использовать данные о вас для персонализированной рекламы.",
                "\uE72E", CatPrivacy, new[] { PrivacyHku },
                () => RegistryHelper.ReadInt(PrivacyHku, "TailoredExperiencesWithDiagnosticDataEnabled", 1) == 0,
                v => RegistryHelper.WriteInt(PrivacyHku, "TailoredExperiencesWithDiagnosticDataEnabled", v ? 0 : 1)));

            list.Add(Reg("location", "Отключить определение местоположения",
                "Запрет доступа приложений к геолокации.",
                "\uE81D", CatPrivacy, new[] { Consent + @"\location" },
                () => ConsentDenied("location"),
                v => SetConsent("location", v)));

            list.Add(Reg("camera", "Отключить доступ к камере",
                "Запрет доступа приложений к веб-камере.",
                "\uE7F4", CatPrivacy, new[] { Consent + @"\webcam" },
                () => ConsentDenied("webcam"),
                v => SetConsent("webcam", v)));

            list.Add(Reg("microphone", "Отключить доступ к микрофону",
                "Запрет доступа приложений к микрофону.",
                "\uE720", CatPrivacy, new[] { Consent + @"\microphone" },
                () => ConsentDenied("microphone"),
                v => SetConsent("microphone", v)));

            list.Add(Reg("notifications", "Отключить уведомления приложений",
                "Запрет доступа приложений к уведомлениям.",
                "\uE7EA", CatPrivacy, new[] { Consent + @"\notifications" },
                () => ConsentDenied("notifications"),
                v => SetConsent("notifications", v)));

            list.Add(Reg("cortana", "Отключить Кортану",
                "Отключает Кортану политикой групповой политики.",
                "\uE8A8", CatPrivacy, new[] { SearchPol },
                () => RegistryHelper.ReadInt(SearchPol, "AllowCortana", 1) == 0,
                v =>
                {
                    RegistryHelper.EnsureKey(SearchPol);
                    RegistryHelper.WriteInt(SearchPol, "AllowCortana", v ? 0 : 1);
                }));

            list.Add(Reg("bing_search", "Отключить веб-поиск",
                "Отключает поиск в интернете из меню Пуск.",
                "\uE721", CatPrivacy, new[] { SearchHku },
                () => RegistryHelper.ReadInt(SearchHku, "BingSearchEnabled", 1) == 0 &&
                      RegistryHelper.ReadInt(SearchHku, "CortanaConsent", 1) == 0,
                v =>
                {
                    RegistryHelper.WriteInt(SearchHku, "BingSearchEnabled", v ? 0 : 1);
                    RegistryHelper.WriteInt(SearchHku, "CortanaConsent", v ? 0 : 1);
                }));

            list.Add(Reg("activity_history", "Отключить журнал активности",
                "Отключает отправку и публикацию журнала активности.",
                "\uE9D5", CatPrivacy, new[] { SysPol },
                () => RegistryHelper.ReadInt(SysPol, "EnableActivityFeed", 1) == 0 &&
                      RegistryHelper.ReadInt(SysPol, "PublishUserActivities", 1) == 0 &&
                      RegistryHelper.ReadInt(SysPol, "UploadUserActivities", 1) == 0,
                v =>
                {
                    RegistryHelper.EnsureKey(SysPol);
                    RegistryHelper.WriteInt(SysPol, "EnableActivityFeed", v ? 0 : 1);
                    RegistryHelper.WriteInt(SysPol, "PublishUserActivities", v ? 0 : 1);
                    RegistryHelper.WriteInt(SysPol, "UploadUserActivities", v ? 0 : 1);
                }));

            list.Add(Reg("wer", "Отключить журнал ошибок (WER)",
                "Отключает отправку отчётов об ошибках Windows.",
                "\uE72E", CatPrivacy, new[] { Wer },
                () => RegistryHelper.ReadInt(Wer, "Disabled", 0) == 1,
                v => RegistryHelper.WriteInt(Wer, "Disabled", v ? 1 : 0)));

            list.Add(Reg("start_ads", "Отключить рекламу в системе",
                "Отключает рекламные предложения в меню Пуск и на экране блокировки.",
                "\uE72E", CatPrivacy, new[] { ContentDelivery },
                () => RegistryHelper.ReadInt(ContentDelivery, "SoftLandingEnabled", 1) == 0 &&
                      RegistryHelper.ReadInt(ContentDelivery, "SubscribedContentEnabled", 1) == 0,
                v =>
                {
                    RegistryHelper.WriteInt(ContentDelivery, "SoftLandingEnabled", v ? 0 : 1);
                    RegistryHelper.WriteInt(ContentDelivery, "SubscribedContentEnabled", v ? 0 : 1);
                    RegistryHelper.WriteInt(ContentDelivery, "SystemPaneSuggestionsEnabled", v ? 0 : 1);
                }));

            // ================== Твики системы ==================
            list.Add(Reg("classic_context", "Классическое контекстное меню",
                "Вернуть классическое полное контекстное меню (Windows 11).",
                "\uE8B7", CatTweaks, new[] { ContextMenu },
                () => RegistryHelper.ReadString(ContextMenu, "") == "",
                v =>
                {
                    if (v)
                    {
                        RegistryHelper.EnsureKey(ContextMenu);
                        RegistryHelper.WriteString(ContextMenu, "", "");
                    }
                    else
                    {
                        RegistryHelper.DeleteKey(ContextMenu);
                    }
                }));

            list.Add(Reg("no_auto_reboot", "Не перезагружаться после обновлений",
                "Windows не будет автоматически перезагружаться при установке обновлений.",
                "\uE916", CatTweaks, new[] { WuAu },
                () => RegistryHelper.ReadInt(WuAu, "NoAutoRebootWithLoggedOnUsers", 0) == 1,
                v =>
                {
                    RegistryHelper.EnsureKey(WuAu);
                    RegistryHelper.WriteInt(WuAu, "NoAutoRebootWithLoggedOnUsers", v ? 1 : 0);
                }));

            list.Add(Reg("bsod_restart", "Отключить перезагрузку при BSOD",
                "Система не будет автоматически перезагружаться при критических ошибках.",
                "\uE7BA", CatTweaks, new[] { Crash },
                () => RegistryHelper.ReadInt(Crash, "AutoReboot", 1) == 0,
                v => RegistryHelper.WriteInt(Crash, "AutoReboot", v ? 0 : 1)));

            list.Add(Reg("disable_uac", "Отключить контроль учётных записей (UAC)",
                "Полное отключение UAC. Требуется перезагрузка. Внимание: снижает безопасность системы!",
                "\uE7BA", CatTweaks, new[] { PolSystem },
                () => RegistryHelper.ReadInt(PolSystem, "EnableLUA", 1) == 0,
                v => RegistryHelper.WriteInt(PolSystem, "EnableLUA", v ? 0 : 1),
                warn: true));

            list.Add(Reg("disable_ipv6", "Отключить IPv6",
                "Отключает протокол IPv6. Требуется перезагрузка.",
                "\uE968", CatTweaks, new[] { Tcpip6 },
                () => RegistryHelper.ReadInt(Tcpip6, "DisabledComponents", 0) == 0xFF,
                v => RegistryHelper.WriteInt(Tcpip6, "DisabledComponents", v ? 0xFF : 0),
                warn: true));

            list.Add(Reg("widgets", "Отключить виджеты (Windows 11)",
                "Убирает кнопку виджетов с панели задач.",
                "\uE9D5", CatTweaks, new[] { ExpAdv },
                () => RegistryHelper.ReadInt(ExpAdv, "TaskbarDa", 0) == 0,
                v => RegistryHelper.WriteInt(ExpAdv, "TaskbarDa", v ? 0 : 1)));

            list.Add(Reg("balloon_tips", "Отключить всплывающие подсказки",
                "Отключает всплывающие подсказки в системном трее.",
                "\uE946", CatTweaks, new[] { ExpAdv },
                () => RegistryHelper.ReadInt(ExpAdv, "EnableBalloonTips", 1) == 0,
                v => RegistryHelper.WriteInt(ExpAdv, "EnableBalloonTips", v ? 0 : 1)));

            foreach (Tweak t in list) t.Refresh();

            return list;
        }

        private static bool IsHighPerformanceActive()
        {
            string output = ProcessHelper.Run("powercfg.exe", "/getactivescheme", false, 10000);
            return output != null && output.Contains(HighPerfGuid);
        }

        private static bool ConsentDenied(string name)
        {
            return RegistryHelper.ReadString(Consent + @"\" + name, "Value", "Allow") == "Deny";
        }

        private static void SetConsent(string name, bool denied)
        {
            string path = Consent + @"\" + name;
            RegistryHelper.EnsureKey(path);
            RegistryHelper.WriteString(path, "Value", denied ? "Deny" : "Allow");
        }
    }
}

