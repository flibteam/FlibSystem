using System.Windows.Controls;

namespace FlibSystem.Views
{
    public partial class PersonalizationView : UserControl
    {
        public PersonalizationView()
        {
            InitializeComponent();
            Rows.ItemsSource = Services.TweakCatalog.Personalization;
        }
    }
}