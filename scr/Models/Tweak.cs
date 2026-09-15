using System;
using System.Windows;

namespace FlibSystem.Models
{
    public class Tweak : ObservableObject
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Glyph { get; set; }
        public string Category { get; set; }
        public bool HasWarning { get; set; }
        public Func<bool> Getter { get; set; }
        public Action<bool> Setter { get; set; }
        public string[] RegistryPaths { get; set; }

        private bool _isApplied;
        public bool IsApplied
        {
            get { return _isApplied; }
            set
            {
                if (_isApplied == value) return;
                _isApplied = value;
                Raise(nameof(IsApplied));

                try
                {
                    Setter?.Invoke(value);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Не удалось применить «" + Name + "»:\n" + ex.Message,
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    try { _isApplied = Getter?.Invoke() ?? false; }
                    catch { _isApplied = false; }
                    Raise(nameof(IsApplied));
                }
            }
        }

        public void Refresh()
        {
            try { _isApplied = Getter?.Invoke() ?? false; }
            catch { _isApplied = false; }
            Raise(nameof(IsApplied));
        }
    }
}

