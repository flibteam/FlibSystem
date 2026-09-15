using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using FlibSystem.Models;
using FlibSystem.Services;
using FlibSystem.Views;

namespace FlibSystem
{
    public partial class MainWindow : Window
    {
        private readonly Dictionary<Type, FrameworkElement> _viewCache = new Dictionary<Type, FrameworkElement>();
        private Rect? _restoreBounds;

        public MainWindow()
        {
            InitializeComponent();

            NavList.ItemsSource = new List<NavPage>
            {
                new NavPage { Title = "Главная", Glyph = "\uE80F", View = typeof(DashboardView) },
                new NavPage { Title = "О системе", Glyph = "\uE946", View = typeof(SystemInfoView) },
                new NavPage { Title = "Персонализация", Glyph = "\uE790", View = typeof(PersonalizationView) },
                new NavPage { Title = "Производительность", Glyph = "\uE945", View = typeof(PerformanceView) },
                new NavPage { Title = "Конфиденциальность", Glyph = "\uE890", View = typeof(PrivacyView) },
                new NavPage { Title = "Твики системы", Glyph = "\uE713", View = typeof(TweaksView) },
                new NavPage { Title = "Диспетчер задач", Glyph = "\uE70B", View = typeof(ProcessesView) },
                new NavPage { Title = "Автозагрузка", Glyph = "\uE7E8", View = typeof(StartupView) },
                new NavPage { Title = "Очистка системы", Glyph = "\uE74D", View = typeof(CleanupView) },
                new NavPage { Title = "Приложения", Glyph = "\uE7C4", View = typeof(UwpView) },
                new NavPage { Title = "Резервные копии", Glyph = "\uE74E", View = typeof(BackupView) },
                new NavPage { Title = "О программе", Glyph = "\uE8FD", View = typeof(AboutView) }
            };

            NavList.SelectedIndex = 0;
        }

        private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NavList.SelectedItem is NavPage page && page.View != null)
            {
                FrameworkElement view;
                if (!_viewCache.TryGetValue(page.View, out view) || view == null)
                {
                    view = (FrameworkElement)Activator.CreateInstance(page.View);
                    _viewCache[page.View] = view;
                }
                ContentHost.Content = view;
                PageTitle.Text = page.Title;
                ContentScroll?.ScrollToTop();
            }
        }

        private async void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string dir = await System.Threading.Tasks.Task.Run(() => BackupService.CreateBackup());
                MessageBox.Show(
                    "Резервная копия создана:\n" + dir,
                    "Резервное копирование",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось создать резервную копию:\n" + ex.Message,
                    "Резервное копирование",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ==================== Кнопки окна (traffic lights) ====================

        private void TrafficLight_Click(object sender, RoutedEventArgs e)
        {
            string tag = (sender as FrameworkElement)?.Tag as string;
            switch (tag)
            {
                case "close": Close(); break;
                case "min": WindowState = WindowState.Minimized; break;
                case "max": ToggleMaximize(); break;
            }
        }

        private void ToggleMaximize()
        {
            if (IsMaximizedState)
            {
                RestoreWindow();
            }
            else
            {
                _restoreBounds = new Rect(Left, Top, ActualWidth, ActualHeight);
                var wa = SystemParameters.WorkArea;
                Left = wa.Left;
                Top = wa.Top;
                Width = wa.Width;
                Height = wa.Height;
                RootBorder.Margin = new Thickness(0);
                MaxBtn.Content = "\uE923";
                MaxBtn.ToolTip = "Свернуть в окно";
            }
        }

        private void RestoreWindow()
        {
            if (_restoreBounds.HasValue)
            {
                Left = _restoreBounds.Value.X;
                Top = _restoreBounds.Value.Y;
                Width = _restoreBounds.Value.Width;
                Height = _restoreBounds.Value.Height;
            }
            RootBorder.Margin = new Thickness(0);
            MaxBtn.Content = "\uE922";
            MaxBtn.ToolTip = "Развернуть";
        }

        private bool IsMaximizedState
        {
            get
            {
                var wa = SystemParameters.WorkArea;
                return Math.Abs(Left - wa.Left) < 1 &&
                       Math.Abs(Top - wa.Top) < 1 &&
                       Math.Abs(Width - wa.Width) < 1 &&
                       Math.Abs(Height - wa.Height) < 1;
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
                return;
            }
            try { DragMove(); } catch { }
        }

        private void TitleBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized) RootBorder.Margin = new Thickness(7);
            else RootBorder.Margin = new Thickness(0);
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            NativeMethods.ApplyWindowChrome(new WindowInteropHelper(this).Handle);
        }
    }
}