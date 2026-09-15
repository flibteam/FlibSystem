using System.Windows.Controls;

namespace FlibSystem.Views
{
    public partial class PerformanceView : UserControl
    {
        public PerformanceView()
        {
            InitializeComponent();
            Rows.ItemsSource = Services.TweakCatalog.Performance;
        }
    }
}