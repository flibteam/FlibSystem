namespace FlibSystem.Models
{
    public class UwpApp : ObservableObject
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string PackageFullName { get; set; }
        public string Publisher { get; set; }
        public string InstallLocation { get; set; }

        private bool _isSelected;
        public bool IsSelected { get { return _isSelected; } set { Set(ref _isSelected, value); } }
    }
}

