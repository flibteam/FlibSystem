using System.Windows.Controls;

namespace FlibSystem.Views
{
    public partial class TweaksView : UserControl
    {
        public TweaksView()
        {
            InitializeComponent();
            Rows.ItemsSource = Services.TweakCatalog.Tweaks;
        }
    }
}