using System.Windows;
using System.Windows.Controls;
using FlibSystem.Models;
using FlibSystem.Services;

namespace FlibSystem.Views
{
    public partial class BackupView : UserControl
    {
        public BackupView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadBackups();
        }

        private void LoadBackups()
        {
            var backups = BackupService.GetBackups();
            BackupList.ItemsSource = backups;
            Status.Text = backups.Count == 0
                ? "Резервных копий пока нет."
                : "Резервных копий: " + backups.Count + ". Папка: " + BackupService.BaseDir;
        }

        private async void Create_Click(object sender, RoutedEventArgs e)
        {
            CreateBtn.IsEnabled = false;
            Status.Text = "Экспорт веток реестра...";
            string dir = await System.Threading.Tasks.Task.Run(() => BackupService.CreateBackup());
            Status.Text = "Копия создана: " + dir;
            CreateBtn.IsEnabled = true;
            LoadBackups();
            MessageBox.Show("Резервная копия создана:\n" + dir, "Резервное копирование",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Restore_Click(object sender, RoutedEventArgs e)
        {
            if (!(BackupList.SelectedItem is BackupEntry entry))
            {
                MessageBox.Show("Выберите резервную копию для восстановления.", "Восстановление",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show(
                "Восстановить настройки из копии\n" + entry.DisplayName + "?\n\n" +
                "Текущие значения будут перезаписаны. Часть изменений применится после перезагрузки.",
                "Восстановление", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            RestoreBtn.IsEnabled = false;
            Status.Text = "Импорт реестра...";
            BackupService.Restore(entry.Folder);
            Status.Text = "Восстановление завершено. Рекомендуется перезагрузка системы.";
            RestoreBtn.IsEnabled = true;
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            BackupService.OpenFolder();
        }
    }
}

