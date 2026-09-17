using System;
using System.Linq;
using System.Security.Principal;
using System.Windows;

namespace FlibSystem
{
    public partial class App : Application
    {
        private const string ThemeRegKey = @"Software\FlibSystem";
        private const string ThemeRegValue = "DarkTheme";

        public static bool IsDarkTheme { get; private set; }

        public static bool IsAdministrator()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public static void ApplyTheme(bool dark)
        {
            IsDarkTheme = dark;

            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(ThemeRegKey))
                {
                    key?.SetValue(ThemeRegValue, dark ? 1 : 0, Microsoft.Win32.RegistryValueKind.DWord);
                }
            }
            catch
            {
            }

            var merged = Current.Resources.MergedDictionaries;
            if (merged == null) return;

            int idx = -1;
            for (int i = 0; i < merged.Count; i++)
            {
                var src = merged[i].Source;
                if (src != null && !string.IsNullOrEmpty(src.OriginalString))
                {
                    string s = src.OriginalString;
                    if (s.Contains("Brushes") && s.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
                    {
                        idx = i;
                        break;
                    }
                }
            }

            string source = dark ? "Themes/BrushesDark.xaml" : "Themes/Brushes.xaml";
            var replacement = new ResourceDictionary
            {
                Source = new Uri(source, UriKind.Relative)
            };

            if (idx >= 0) merged[idx] = replacement;
            else merged.Insert(0, replacement);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            bool dark = false;
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(ThemeRegKey))
                {
                    object v = key?.GetValue(ThemeRegValue);
                    if (v != null) dark = (Convert.ToInt32(v) != 0);
                }
            }
            catch
            {
            }
            if (dark) ApplyTheme(true);

            base.OnStartup(e);

            DispatcherUnhandledException += OnDispatcherUnhandledException;
            System.AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

            if (!IsAdministrator())
            {
                MessageBox.Show(
                    "FlibSystem требует прав администратора.\n\n" +
                    "Запустите приложение от имени администратора.",
                    "Нет прав администратора",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                Shutdown();
                return;
            }

            ShutdownMode = ShutdownMode.OnMainWindowClose;
        }

        private void OnDispatcherUnhandledException(object sender,
            System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                "Произошла непредвиденная ошибка:\n" + e.Exception,
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void OnDomainUnhandledException(object sender,
            System.UnhandledExceptionEventArgs e)
        {
            try
            {
                if (e.ExceptionObject is System.Exception ex)
                {
                    MessageBox.Show(
                        "Произошла критическая ошибка:\n" + ex,
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch
            {
            }
        }
    }
}

