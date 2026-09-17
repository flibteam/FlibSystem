using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FlibSystem.Models;
using FlibSystem.Services;

namespace FlibSystem.Views
{
    public partial class AboutView : UserControl
    {
        private ReleaseInfo _latestRelease;
        private bool _busy;

        public AboutView()
        {
            InitializeComponent();
            UpdateStatusText.Text = "Текущая версия: " + UpdateService.CurrentVersionText;
            DarkThemeToggle.IsChecked = App.IsDarkTheme;
            DarkThemeToggle.Checked += ThemeToggle_Changed;
            DarkThemeToggle.Unchecked += ThemeToggle_Changed;
        }

        private void ThemeToggle_Changed(object sender, RoutedEventArgs e)
        {
            App.ApplyTheme(DarkThemeToggle.IsChecked == true);
        }

        private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            if (_busy) return;
            _busy = true;
            CheckUpdateBtn.IsEnabled = false;
            DownloadUpdateBtn.IsEnabled = false;
            UpdateStatusText.Text = "Проверка обновлений…";

            _latestRelease = await UpdateService.GetLatestReleaseAsync();

            if (_latestRelease == null)
            {
                UpdateStatusText.Text = "Не удалось проверить обновления. Проверьте подключение к интернету.";
            }
            else if (UpdateService.IsCurrentNewer(_latestRelease))
            {
                UpdateStatusText.Text =
                    "Установлена более новая версия программы (" +
                    UpdateService.CurrentVersionText + "), чем в официальных источниках (" +
                    _latestRelease.VersionText + ").\n\n" +
                    "Так как программа имеет открытый исходный код, вашу сборку могли " +
                    "модифицировать и добавить в неё вирусы. Официальная версия проверена " +
                    "и безопасна — лучше перекачать её с GitHub.";
                MessageBox.Show(
                    "Обнаружена более новая версия программы, чем в официальных источниках.\n\n" +
                    "Программа имеет открытый исходный код — вашу сборку могли модифицировать " +
                    "и добавить в неё вредоносный код. Официальная версия проверена и точно " +
                    "без вирусов.\n\nРекомендуем перекачать её с официального GitHub: " +
                    UpdateService.RepoUrl,
                    "Внимание",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            else if (UpdateService.IsNewerAvailable(_latestRelease))
            {
                UpdateStatusText.Text =
                    "Доступна новая версия " + _latestRelease.VersionText +
                    (string.IsNullOrEmpty(_latestRelease.Name) ? "" : " («" + _latestRelease.Name + "»)") +
                    ". Нажмите «Скачать новую версию».";
                DownloadUpdateBtn.IsEnabled = _latestRelease.HasAsset;
            }
            else
            {
                UpdateStatusText.Text = "У вас установлена последняя версия " +
                    UpdateService.CurrentVersionText + ".";
            }

            _busy = false;
            CheckUpdateBtn.IsEnabled = true;
        }

        private void DownloadUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (_latestRelease == null || !_latestRelease.HasAsset) return;
            if (_busy) return;
            _busy = true;

            CheckUpdateBtn.IsEnabled = false;
            DownloadUpdateBtn.IsEnabled = false;
            UpdateProgress.Value = 0;
            UpdateProgress.Visibility = Visibility.Visible;
            UpdateStatusText.Text = "Скачивание…";
            OpenUpdateFolderBtn.Visibility = Visibility.Collapsed;

            string target = UpdateService.GetDownloadsPath(_latestRelease);
            UpdateService.DownloadRelease(_latestRelease, target,
                (received, total) => Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (total > 0)
                    {
                        UpdateProgress.Value = received * 100.0 / total;
                        UpdateStatusText.Text = string.Format("Скачивание… {0:F0}%", received * 100.0 / total);
                    }
                })),
                path => Dispatcher.BeginInvoke(new Action(() => OnDownloadCompleted(path))));
        }

        private async void OnDownloadCompleted(string path)
        {
            _busy = false;
            UpdateProgress.Visibility = Visibility.Collapsed;
            CheckUpdateBtn.IsEnabled = true;

            if (string.IsNullOrEmpty(path))
            {
                UpdateStatusText.Text = "Не удалось скачать обновление. Попробуйте ещё раз.";
                DownloadUpdateBtn.IsEnabled = _latestRelease?.HasAsset == true;
                return;
            }

            UpdateStatusText.Text = "Проверка контрольной суммы…";
            string error = _latestRelease != null
                ? await Task.Run(() => UpdateService.VerifyDownload(path, _latestRelease))
                : null;

            if (error != null)
            {
                UpdateStatusText.Text = error;
                DownloadUpdateBtn.IsEnabled = _latestRelease?.HasAsset == true;
                MessageBox.Show(error, "Проверка обновления",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var sizeText = FormatSize(_latestRelease?.AssetSize ?? 0);
            UpdateStatusText.Text =
                "Обновление скачано и проверено:\n" + path +
                (string.IsNullOrEmpty(sizeText) ? "" : "\nРазмер: " + sizeText);
            OpenUpdateFolderBtn.Visibility = Visibility.Visible;

            var result = MessageBox.Show(
                "Новая версия скачана и проверена по контрольной сумме:\n" +
                path + "\n\nОткрыть папку загрузок?",
                "Обновление", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes) UpdateService.OpenDownloadsFolder();
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            UpdateService.OpenDownloadsFolder();
        }

        private static string FormatSize(long bytes)
        {
            if (bytes <= 0) return null;
            double mb = bytes / 1024.0 / 1024.0;
            if (mb >= 1024) return (mb / 1024.0).ToString("0.00") + " ГБ";
            return mb.ToString("0.0") + " МБ";
        }

        private void GitHub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/flibteam/FlibSystem") { UseShellExecute = true });
        }

        private void Donate_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://www.donationalerts.com/r/jannirobot") { UseShellExecute = true });
        }
    }
}