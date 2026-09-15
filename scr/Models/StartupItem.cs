using System;

namespace FlibSystem.Models
{
    public class StartupItem : ObservableObject
    {
        public string Name { get; set; }
        public string Command { get; set; }
        public string Source { get; set; }
        public string FilePath { get; set; }
        public string RelativePath { get; set; }
        public bool IsFromFolder { get; set; }
        public Func<StartupItem, bool, bool> CommitEnabled { get; set; }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                bool ok = true;
                if (CommitEnabled != null && _isEnabled != value)
                {
                    ok = CommitEnabled(this, value);
                }
                if (ok)
                {
                    _isEnabled = value;
                }
                Raise(nameof(IsEnabled));
            }
        }

        public void SetInitialState(bool value)
        {
            _isEnabled = value;
            Raise(nameof(IsEnabled));
        }

        public string SourceText
        {
            get
            {
                if (IsFromFolder) return "Папка Пуск";
                return Source == "HKLM" ? "HKLM" : "HKCU";
            }
        }
    }
}