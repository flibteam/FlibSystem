using System.Windows;
using System.Windows.Controls;
using FlibSystem.Services;

namespace FlibSystem.Views
{
public partial class SystemInfoView : UserControl
    {
        private bool _initialized;

        public SystemInfoView()
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
            RefreshButton.IsEnabled = false;
            Status.Text = "Сбор информации...";
            var groups = await System.Threading.Tasks.Task.Run(() => SystemInfoService.Gather());
            Groups.ItemsSource = groups;
            Status.Text = "Информация собрана. Обновлено: " + System.DateTime.Now.ToString("HH:mm:ss");
            RefreshButton.IsEnabled = true;
        }
    }
}

