using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FlibSystem.Models;
using FlibSystem.Services;

namespace FlibSystem.Views
{
    public partial class StartupView : UserControl
    {
        private bool _initialized;
        private List<StartupItem> _items;

        public StartupView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_initialized) return;
            _initialized = true;
            Load();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            Load();
        }

        private void Load()
        {
            _items = StartupService.GetItems();
            StartupList.ItemsSource = _items;
            Status.Text = "Элементов автозагрузки: " + _items.Count;
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelected();
            if (selected.Count == 0) return;

            if (MessageBox.Show(
                "Удалить " + selected.Count + " элемент(ов) из автозагрузки?\n" +
                "Для файлов из папки «Пуск» создаётся резервная копия в подпапке.",
                "Автозагрузка", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            int ok = 0;
            foreach (var item in selected)
            {
                if (StartupService.Remove(item)) ok++;
            }
            Load();
            Status.Text = "Удалено элементов: " + ok + " из " + selected.Count;
        }

        private void Reveal_Click(object sender, RoutedEventArgs e)
        {
            var first = GetSelected().FirstOrDefault();
            if (first != null) StartupService.Reveal(first);
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            StartupService.OpenStartupFolder();
        }

        private List<StartupItem> GetSelected()
        {
            var list = new List<StartupItem>();
            if (StartupList.SelectedItems == null) return list;
            foreach (object o in StartupList.SelectedItems)
            {
                if (o is StartupItem item) list.Add(item);
            }
            return list;
        }
    }
}