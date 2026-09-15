using System.ComponentModel;

namespace FlibSystem.Models
{
    public class ProcessItem : INotifyPropertyChanged
    {
        private string _name;
        private int _id;
        private double _cpuPercent;
        private long _memoryMb;
        private double _memoryPercent;
        private int _threads;
        private string _startTime;
        private string _fileName;
        private bool _isProtected;
        private bool _isHung;
        private System.Windows.Media.ImageSource _icon;

        public string Name { get { return _name; } set { _name = value; OnPropertyChanged("Name"); } }
        public int Id { get { return _id; } set { _id = value; OnPropertyChanged("Id"); } }
        public double CpuPercent { get { return _cpuPercent; } set { _cpuPercent = value; OnPropertyChanged("CpuPercent"); OnPropertyChanged("CpuText"); } }
        public long MemoryMb { get { return _memoryMb; } set { _memoryMb = value; OnPropertyChanged("MemoryMb"); OnPropertyChanged("MemoryText"); } }
        public double MemoryPercent { get { return _memoryPercent; } set { _memoryPercent = value; OnPropertyChanged("MemoryPercent"); } }
        public int Threads { get { return _threads; } set { _threads = value; OnPropertyChanged("Threads"); } }
        public string StartTime { get { return _startTime; } set { _startTime = value; OnPropertyChanged("StartTime"); } }
        public string FileName { get { return _fileName; } set { _fileName = value; OnPropertyChanged("FileName"); } }
        public bool IsProtected { get { return _isProtected; } set { _isProtected = value; OnPropertyChanged("IsProtected"); } }
        public bool IsHung { get { return _isHung; } set { _isHung = value; OnPropertyChanged("IsHung"); } }
        public System.Windows.Media.ImageSource Icon { get { return _icon; } set { _icon = value; OnPropertyChanged("Icon"); } }

        public string CpuText { get { return CpuPercent.ToString("0.0") + " %"; } }
        public string MemoryText { get { return MemoryMb.ToString("N0") + " МБ"; } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
