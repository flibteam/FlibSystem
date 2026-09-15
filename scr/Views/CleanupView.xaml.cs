using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FlibSystem.Models;
using FlibSystem.Services;

namespace FlibSystem.Views
{
public partial class CleanupView : UserControl
    {
        private System.Collections.Generic.List<CleanupItem> _items;
        private bool _initialized;

        public CleanupView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_initialized) return;
            _initialized = true;
            _items = CleanupService.BuildItems();
            CleanList.ItemsSource = _items;
        }

        private async void Analyze_Click(object sender, RoutedEventArgs e)
        {
            AnalyzeBtn.IsEnabled = false;
            Status.Text = "Подсчёт размера...";
            var selected = _items.Where(i => i.IsSelected).ToList();

            foreach (var item in selected)
            {
                item.SizeBytes = await System.Threading.Tasks.Task.Run(() => CleanupService.GetSize(item));
            }

            long total = selected.Sum(i => i.SizeBytes);
            Status.Text = "Анализ завершён. Выбрано: " + selected.Count + " элементов, всего " +
                          CleanupItem.FormatSize(total) + ". Нажмите «Очистить выбранное».";
            AnalyzeBtn.IsEnabled = true;
        }

        private async void Clean_Click(object sender, RoutedEventArgs e)
        {
            var selected = _items.Where(i => i.IsSelected).ToList();
            if (selected.Count == 0) return;

            if (MessageBox.Show("Удалить " + selected.Count + " выбранных элементов?",
                "Очистка системы", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            CleanBtn.IsEnabled = false;
            Status.Text = "Очистка...";

            foreach (var item in selected)
            {
                await System.Threading.Tasks.Task.Run(() => CleanupService.Clean(item));
            }

            Status.Text = "Очистка завершена. Нажмите «Анализ» для повторного подсчёта.";
            CleanBtn.IsEnabled = true;
        }
    }
}

