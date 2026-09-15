using System.Windows;
using System.Windows.Controls;

namespace FlibSystem.Controls
{
    public partial class SettingRow : UserControl
    {
        public SettingRow()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(SettingRow), new PropertyMetadata(""));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(string), typeof(SettingRow), new PropertyMetadata(""));

        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        public static readonly DependencyProperty GlyphProperty =
            DependencyProperty.Register("Glyph", typeof(string), typeof(SettingRow), new PropertyMetadata(""));

        public string Glyph
        {
            get { return (string)GetValue(GlyphProperty); }
            set { SetValue(GlyphProperty, value); }
        }

        public static readonly DependencyProperty IsOnProperty =
            DependencyProperty.Register("IsOn", typeof(bool), typeof(SettingRow), new PropertyMetadata(false));

        public bool IsOn
        {
            get { return (bool)GetValue(IsOnProperty); }
            set { SetValue(IsOnProperty, value); }
        }

public static readonly DependencyProperty IsToggleEnabledProperty =
            DependencyProperty.Register("IsToggleEnabled", typeof(bool), typeof(SettingRow), new PropertyMetadata(true));

        public bool IsToggleEnabled
        {
            get { return (bool)GetValue(IsToggleEnabledProperty); }
            set { SetValue(IsToggleEnabledProperty, value); }
        }

        public static readonly DependencyProperty HasWarningProperty =
            DependencyProperty.Register("HasWarning", typeof(bool), typeof(SettingRow), new PropertyMetadata(false));

        public bool HasWarning
        {
            get { return (bool)GetValue(HasWarningProperty); }
            set { SetValue(HasWarningProperty, value); }
        }
    }
}

