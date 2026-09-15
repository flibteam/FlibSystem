using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace FlibSystem.Views
{
    public partial class AboutView : UserControl
    {
        public AboutView()
        {
            InitializeComponent();
        }

        private void GitHub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/flibteam") { UseShellExecute = true });
        }

        private void Donate_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://www.donationalerts.com/r/jannirobot") { UseShellExecute = true });
        }
    }
}