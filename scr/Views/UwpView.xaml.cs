using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FlibSystem.Models;
using FlibSystem.Services;

namespace FlibSystem.Views
{
public partial class UwpView : UserControl
    {
        private List<UwpApp> _apps;
        private bool _initialized;

        public UwpView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_initialized) return;
            _initialized = true;
            await Load();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await Load();
        }

        private async System.Threading.Tasks.Task Load()
        {
            RefreshBtn.IsEnabled = false;
            Status.Text = "Загрузка списка приложений...";
            var apps = await System.Threading.Tasks.Task.Run(() => UwpService.GetApps());
            _apps = apps;
            AppList.ItemsSource = apps;
            Status.Text = "Приложений: " + apps.Count;
            RefreshBtn.IsEnabled = true;
        }

        private void SelectBloat_Click(object sender, RoutedEventArgs e)
        {
            if (_apps == null) return;
            int count = 0;
            foreach (var app in _apps)
            {
                bool bloat = UwpService.IsBloatware(app);
                app.IsSelected = bloat;
                if (bloat) count++;
            }
            Status.Text = "Выделено подозрительных (bloatware): " + count;
        }

        private async void Uninstall_Click(object sender, RoutedEventArgs e)
        {
            var selected = _apps?.Where(a => a.IsSelected).ToList();
            if (selected == null || selected.Count == 0) return;

            if (MessageBox.Show(
                "Удалить " + selected.Count + " приложений?\n" +
                "Перед удалением обязательно создайте резервную копию системы (восстановление возможно только через магазин или ISO).",
                "Удаление приложений", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            UninstallBtn.IsEnabled = false;
            int failed = 0;
            try
            {
                foreach (var app in selected)
                {
                    Status.Text = "Удаление: " + app.Name + " ...";
                    try
                    {
                        await System.Threading.Tasks.Task.Run(() => UwpService.Uninstall(app));
                    }
                    catch
                    {
                        failed++;
                    }
                }

                Status.Text = "Проверка результата удаления...";
                var remaining = await System.Threading.Tasks.Task.Run(() => UwpService.GetInstalledNames());
                int done = selected.Count(a => !remaining.Contains(a.Name));
                failed += selected.Count - done;

                Status.Text = "Удалено: " + done + ", ошибок: " + failed + ".";
                MessageBox.Show(
                    "Удалено: " + done + ", ошибок: " + failed + ".",
                    "Удаление приложений", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось выполнить удаление:\n" + ex.Message,
                    "Удаление приложений", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UninstallBtn.IsEnabled = true;
            }

            await Load();
        }
    }
}

