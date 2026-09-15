using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FlibSystem.Models;
using FlibSystem.Services;

namespace FlibSystem.Views
{
public partial class DashboardView : UserControl
    {
        private bool _initialized;

        public DashboardView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_initialized) return;
            _initialized = true;

            var summary = await System.Threading.Tasks.Task.Run(() => SystemInfoService.GetSummary());
            InfoCards.Children.Add(BuildCard("Процессор", summary.Cpu, "\uE950"));
            InfoCards.Children.Add(BuildCard("Оперативная память", summary.Ram, "\uE8F1"));
            InfoCards.Children.Add(BuildCard("Система", summary.Os, "\uE7FC"));
        }

        private static Border BuildCard(string title, string value, string glyph)
        {
            var card = new Border
            {
                Style = (Style)System.Windows.Application.Current.FindResource("Card"),
                Width = 300,
                Height = 120,
                Margin = new Thickness(0, 0, 12, 12)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var iconBox = new Border
            {
                Background = (Brush)System.Windows.Application.Current.FindResource("AccentDarkBrush"),
                CornerRadius = new CornerRadius(10),
                Width = 40,
                Height = 40,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            iconBox.Child = new TextBlock
            {
                Text = glyph,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 18,
                Foreground = (Brush)System.Windows.Application.Current.FindResource("AccentBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(iconBox, 0);
            grid.Children.Add(iconBox);

            var text = new StackPanel { VerticalAlignment = VerticalAlignment.Top };
            text.Children.Add(new TextBlock
            {
                Text = title,
                Foreground = (Brush)System.Windows.Application.Current.FindResource("SecondaryTextBrush"),
                FontSize = 12,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            text.Children.Add(new TextBlock
            {
                Text = value,
                Foreground = (Brush)System.Windows.Application.Current.FindResource("PrimaryTextBrush"),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0)
            });
            Grid.SetColumn(text, 1);
            grid.Children.Add(text);

            card.Child = grid;
            return card;
        }

        private void Backup_Click(object sender, RoutedEventArgs e)
        {
            string dir = BackupService.CreateBackup();
            MessageBox.Show("Резервная копия создана:\n" + dir, "Резервное копирование",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CleanTemp_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Удалить содержимое временных папок (%TEMP% и C:\\Windows\\Temp)?",
                "Очистка временных файлов", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                CleanupService.CleanTemp();
                MessageBox.Show("Временные файлы удалены.", "Очистка",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Не удалось полностью очистить:\n" + ex.Message, "Очистка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RestartExplorer_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Перезапустить Проводник? Открытые окна проводника будут закрыты.",
                "Перезапуск Проводника", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;
            ExplorerHelper.RestartExplorer();
        }

        private void OpenBackupFolder_Click(object sender, RoutedEventArgs e)
        {
            BackupService.OpenFolder();
        }
    }
}

