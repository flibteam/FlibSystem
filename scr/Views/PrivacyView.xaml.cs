using System.Windows.Controls;

namespace FlibSystem.Views
{
    public partial class PrivacyView : UserControl
    {
        public PrivacyView()
        {
            InitializeComponent();
            Rows.ItemsSource = Services.TweakCatalog.Privacy;
        }
    }
}