using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using FlibSystem.Models;
using FlibSystem.Services;

namespace FlibSystem.Views
{
    public partial class ProcessesView : UserControl
    {
        private bool _initialized;
        private bool _isProcsTab = true;
        private bool _paused;

        private DispatcherTimer _procTimer;
        private DispatcherTimer _monTimer;
        private MonitorService _monService;

        // Monitor data queues (last N samples)
        private const int GraphPoints = 90;
        private readonly Queue<double> _cpuQ = new Queue<double>();
        private readonly Queue<double> _ramQ = new Queue<double>();
        private readonly Queue<double> _netDownQ = new Queue<double>();
        private readonly Queue<double> _netUpQ = new Queue<double>();
        private readonly Queue<double> _diskQ = new Queue<double>();
        private readonly Queue<double> _gpuQ = new Queue<double>();

        // Current monitor string
        private string _activeMonitor = "Cpu";

        // Process list
        private readonly object _sampleLock = new object();
        private List<ProcessItem> _allItems = new List<ProcessItem>();
        private int _memFilterMb;

        public ProcessesView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_initialized) { StartTimers(); return; }
            _initialized = true;

            _monService = new MonitorService();

            _procTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _procTimer.Tick += ProcTimer_Tick;

            _monTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _monTimer.Tick += MonTimer_Tick;

            await System.Threading.Tasks.Task.Run(() => RefreshProcessList());
            StartTimers();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            StopTimers();
        }

        private void StartTimers()
        {
            if (_procTimer == null || _monTimer == null) return;
            if (_isProcsTab && !_paused)
            {
                if (!_procTimer.IsEnabled) _procTimer.Start();
                if (_monTimer.IsEnabled) _monTimer.Stop();
            }
            else if (!_isProcsTab && !_paused)
            {
                if (_procTimer.IsEnabled) _procTimer.Stop();
                if (!_monTimer.IsEnabled) _monTimer.Start();
            }
        }

        private void StopTimers()
        {
            if (_procTimer != null) _procTimer.Stop();
            if (_monTimer != null) _monTimer.Stop();
        }

        // ==================== Tab switching ====================

        private void ProcsTab_Click(object sender, RoutedEventArgs e)
        {
            _isProcsTab = true;
            MonTabBtn.IsChecked = false;
            ProcsPanel.Visibility = Visibility.Visible;
            MonPanel.Visibility = Visibility.Collapsed;
            StartTimers();
        }

        private void MonTab_Click(object sender, RoutedEventArgs e)
        {
            _isProcsTab = false;
            ProcsTabBtn.IsChecked = false;
            ProcsPanel.Visibility = Visibility.Collapsed;
            MonPanel.Visibility = Visibility.Visible;
            StartTimers();
        }

        // ==================== Process list ====================

        private async void ProcTimer_Tick(object sender, EventArgs e)
        {
            await System.Threading.Tasks.Task.Run(() => RefreshProcessList());
        }

        private void RefreshProcessList()
        {
            List<ProcessItem> items;
            lock (_sampleLock)
            {
                try { items = ProcessService.GetLiveSnapshot(); }
                catch { return; }
            }
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _allItems = items;
                ApplyFilter();
            }));
        }

        private void ApplyFilter()
        {
            var selectedIds = new HashSet<int>();
            if (ProcList.SelectedItems != null)
            {
                foreach (var o in ProcList.SelectedItems)
                    if (o is ProcessItem p) selectedIds.Add(p.Id);
            }

            var filtered = _allItems;
            if (_memFilterMb > 0)
                filtered = filtered.Where(p => p.MemoryMb >= _memFilterMb).ToList();

            filtered = filtered
                .OrderByDescending(p => p.CpuPercent)
                .ThenByDescending(p => p.MemoryMb)
                .ToList();

            ProcList.ItemsSource = filtered;

            if (selectedIds.Count > 0)
            {
                ProcList.SelectedItems.Clear();
                foreach (var item in filtered)
                    if (selectedIds.Contains(item.Id))
                        ProcList.SelectedItems.Add(item);
            }

            long totalMem = filtered.Sum(p => p.MemoryMb);
            ProcCountText.Text = "Процессов: " + filtered.Count;
            ProcMemText.Text = "Память: " + FormatBytes(totalMem * 1024 * 1024);
            StatusText.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await System.Threading.Tasks.Task.Run(() => RefreshProcessList());
        }

        private void AutoRefresh_Changed(object sender, RoutedEventArgs e)
        {
            if (AutoRefreshCb == null) return;
            _paused = !AutoRefreshCb.IsChecked.GetValueOrDefault();
            if (_paused) StopTimers();
            else StartTimers();
        }

        private void MemFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (MemFilterCombo == null || ProcList == null) return;
            var item = MemFilterCombo.SelectedItem as ComboBoxItem;
            if (item == null) return;
            int.TryParse(item.Tag as string, out _memFilterMb);
            ApplyFilter();
        }

        // ==================== Kill / Open location ====================

        private void Kill_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelected();
            if (selected.Count == 0) return;

            var blocked = selected.Where(p => p.IsProtected).ToList();
            var killable = selected.Where(p => !p.IsProtected).ToList();

            if (blocked.Count > 0)
            {
                string names = string.Join(", ", blocked.Take(5).Select(p => p.Name));
                if (blocked.Count > 5) names += " и др.";
                MessageBox.Show(
                    "Нельзя завершить системные процессы:\n" + names,
                    "Завершение запрещено", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            if (killable.Count == 0) return;

            if (MessageBox.Show("Завершить " + killable.Count + " процесс(ов)?",
                "Завершение", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            foreach (var item in killable) ProcessService.Kill(item);
            Refresh_Click(null, null);
        }

        private void OpenLocation_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelected();
            if (selected.Count > 0) ProcessService.OpenFileLocation(selected[0]);
        }

        private void ProcList_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Delete)
                Kill_Click(null, null);
        }

        private List<ProcessItem> GetSelected()
        {
            var list = new List<ProcessItem>();
            if (ProcList?.SelectedItems == null) return list;
            foreach (var o in ProcList.SelectedItems)
            {
                if (o is ProcessItem p) list.Add(p);
            }
            return list;
        }

        // ==================== Monitor ====================

        private void MonTimer_Tick(object sender, EventArgs e)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var sample = _monService.Sample();
                    Dispatcher.BeginInvoke(new Action(() => UpdateMonitor(sample)));
                }
                catch { }
            });
        }

        private void UpdateMonitor(MonitorSample s)
        {
            // Update nav values
            CpuNavValue.Text = s.CpuPercent.ToString("0") + " %";
            RamNavValue.Text = s.RamPercent.ToString("0") + " %  " + FormatMb(s.RamUsedMb) + "/" + FormatMb(s.RamTotalMb);

            double netTotalBps = s.NetDownBps + s.NetUpBps;
            NetNavValue.Text = FormatBps(netTotalBps);

            DiskNavValue.Text = s.DiskPercent.ToString("0") + " %";
            GpuNavValue.Text = s.GpuPercent >= 0 ? s.GpuPercent.ToString("0") + " %" : "Н/Д";

            // Update graphs
            EnqueueAndDraw(_cpuQ, s.CpuPercent, CpuGraphCanvas);
            EnqueueAndDraw(_ramQ, s.RamPercent, RamGraphCanvas);
            EnqueueAndDraw(_netDownQ, s.NetDownBps / 1024.0, NetGraphCanvas);
            EnqueueAndDraw(_netUpQ, s.NetUpBps / 1024.0, NetGraphCanvas);
            EnqueueAndDraw(_diskQ, s.DiskPercent, DiskGraphCanvas);
            EnqueueAndDraw(_gpuQ, s.GpuPercent >= 0 ? s.GpuPercent : 0, GpuGraphCanvas);

            // Detail values
            ShowDetail(s);
        }

        private void ShowDetail(MonitorSample s)
        {
            if (_activeMonitor == "Cpu")
            {
                CpuBigValue.Text = s.CpuPercent.ToString("0.0") + " %";
                CpuFreqValue.Text = s.CpuClockMHz > 0 ? s.CpuClockMHz.ToString("0") + " MHz" : "—";
                CpuBaseValue.Text = "Базовая " + s.CpuBaseMHz.ToString("0") + " MHz";
                CpuProcessesValue.Text = "Процессов: " + s.ProcessCount;
                CpuUptimeValue.Text = "Работа: " + FormatUptime(s.UptimeHours);

                // Per-core bars
                if (s.CpuPerCore != null && s.CpuPerCore.Length > 0)
                {
                    var items = new List<CoreBar>();
                    for (int i = 0; i < s.CpuPerCore.Length; i++)
                    {
                        items.Add(new CoreBar
                        {
                            Label = "Ядро " + i + " — " + s.CpuPerCore[i].ToString("0") + "%",
                            BarWidth = Math.Max(0, s.CpuPerCore[i] * 0.92)
                        });
                    }
                    CpuCoresPanel.ItemsSource = items;
                }
            }
            else if (_activeMonitor == "Ram")
            {
                RamBigValue.Text = s.RamPercent.ToString("0.0") + " %";
                RamUsedValue.Text = "Используется " + FormatMb(s.RamUsedMb) + " / " + FormatMb(s.RamTotalMb);
                RamTotalValue.Text = "Всего " + FormatMb(s.RamTotalMb);
            }
            else if (_activeMonitor == "Net")
            {
                NetBigDown.Text = FormatBps(s.NetDownBps);
                NetBigUp.Text = FormatBps(s.NetUpBps);
            }
            else if (_activeMonitor == "Disk")
            {
                DiskBigValue.Text = s.DiskPercent.ToString("0.0") + " %";
            }
            else if (_activeMonitor == "Gpu")
            {
                GpuBigValue.Text = s.GpuPercent >= 0 ? s.GpuPercent.ToString("0.0") + " %" : "Нет данных";
                GpuMemValue.Text = s.GpuMemTotalMb > 0
                    ? "Видеопамять: " + FormatMb((long)s.GpuMemUsedMb) + " / " + FormatMb((long)s.GpuMemTotalMb)
                    : "";
            }
        }

        private void MonNav_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MonNavList == null || CpuDetailPanel == null) return;
            var item = MonNavList.SelectedItem as ListBoxItem;
            if (item == null) return;
            _activeMonitor = (item.Tag as string) ?? "Cpu";

            CpuDetailPanel.Visibility = _activeMonitor == "Cpu" ? Visibility.Visible : Visibility.Collapsed;
            RamDetailPanel.Visibility = _activeMonitor == "Ram" ? Visibility.Visible : Visibility.Collapsed;
            NetDetailPanel.Visibility = _activeMonitor == "Net" ? Visibility.Visible : Visibility.Collapsed;
            DiskDetailPanel.Visibility = _activeMonitor == "Disk" ? Visibility.Visible : Visibility.Collapsed;
            GpuDetailPanel.Visibility = _activeMonitor == "Gpu" ? Visibility.Visible : Visibility.Collapsed;
        }

        // ==================== Graph drawing ====================

        private void EnqueueAndDraw(Queue<double> q, double value, Canvas canvas)
        {
            q.Enqueue(value);
            while (q.Count > GraphPoints) q.Dequeue();
            DrawGraph(q, canvas);
        }

        private void DrawGraph(Queue<double> q, Canvas canvas)
        {
            if (canvas.ActualWidth < 1 || canvas.ActualHeight < 1 || q.Count < 2) return;

            canvas.Children.Clear();

            double w = canvas.ActualWidth;
            double h = canvas.ActualHeight;
            double max = q.Max();
            if (max <= 0) max = 100;

            // Horizontal grid lines
            for (int i = 1; i <= 3; i++)
            {
                double y = h * i / 4.0;
                var gridLine = new Line { X1 = 0, X2 = w, Y1 = y, Y2 = y, Stroke = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0)), StrokeThickness = 0.5 };
                canvas.Children.Add(gridLine);
            }

            // Data polyline
            var pts = new PointCollection();
            int count = q.Count;
            double[] arr = q.ToArray();
            for (int i = 0; i < count; i++)
            {
                double x = w * i / (GraphPoints - 1.0);
                double y = h - (arr[i] / max * h);
                y = Math.Max(0, Math.Min(h, y));
                pts.Add(new Point(x, y));
            }

            var line = new Polyline
            {
                Stroke = (Brush)FindResource("AccentBrush"),
                StrokeThickness = 1.8,
                StrokeLineJoin = PenLineJoin.Round,
                Points = pts
            };
            canvas.Children.Add(line);

            // Area fill
            var areaPts = new PointCollection(pts);
            areaPts.Insert(0, new Point(pts[0].X, h));
            areaPts.Add(new Point(pts[pts.Count - 1].X, h));

            var fill = new Polygon
            {
                Points = areaPts,
                Fill = new LinearGradientBrush(
                    Color.FromArgb(40, 10, 132, 255),
                    Color.FromArgb(8, 10, 132, 255),
                    0),
                StrokeThickness = 0
            };
            canvas.Children.Insert(0, fill);
        }

        // ==================== Formatting ====================

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1073741824) return (bytes / 1073741824.0).ToString("0.#") + " ГБ";
            if (bytes >= 1048576) return (bytes / 1048576.0).ToString("0.#") + " МБ";
            return (bytes / 1024.0).ToString("0.#") + " КБ";
        }

        private static string FormatMb(long mb)
        {
            if (mb >= 1024) return (mb / 1024.0).ToString("0.#") + " ГБ";
            return mb + " МБ";
        }

        private static string FormatBps(double bps)
        {
            if (bps >= 1048576) return (bps / 1048576.0).ToString("0.#") + " МБ/с";
            if (bps >= 1024) return (bps / 1024.0).ToString("0.#") + " КБ/с";
            return bps.ToString("0") + " Б/с";
        }

        private static string FormatUptime(double hours)
        {
            int h = (int)hours;
            int m = (int)((hours - h) * 60);
            if (h > 24)
            {
                int d = h / 24;
                h = h % 24;
                return d + " д " + h + " ч " + m + " мин";
            }
            return h + " ч " + m + " мин";
        }

        private class CoreBar
        {
            public string Label { get; set; }
            public double BarWidth { get; set; }
        }
    }
}
