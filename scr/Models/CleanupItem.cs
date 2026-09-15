using System;

namespace FlibSystem.Models
{
    public class CleanupItem : ObservableObject
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Kind { get; set; }
        public Func<string> GetPath { get; set; }
        public string PathText { get { try { return GetPath != null ? GetPath() : ""; } catch { return ""; } } }

        private bool _isSelected = true;
        public bool IsSelected { get { return _isSelected; } set { Set(ref _isSelected, value); } }

        private long _sizeBytes;
        public long SizeBytes { get { return _sizeBytes; } set { Set(ref _sizeBytes, value); } }

        public string SizeText
        {
            get { return _sizeBytes <= 0 ? "—" : FormatSize(_sizeBytes); }
        }

        public string KindText
        {
            get { return Kind == "recycle" ? "Корзина" : "Папка"; }
        }

        public static string FormatSize(long bytes)
        {
            double value = bytes;
            string[] units = { "Б", "КБ", "МБ", "ГБ", "ТБ" };
            int i = 0;
            while (value >= 1024 && i < units.Length - 1) { value /= 1024; i++; }
            return value.ToString(i == 0 ? "0" : "0.##") + " " + units[i];
        }
    }
}

