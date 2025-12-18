using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace QuickLaunch.UI.Controls
{
    public class PlaceholderTextBox : System.Windows.Controls.TextBox
    {
        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(PlaceholderTextBox), new PropertyMetadata(string.Empty));

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public PlaceholderTextBox()
        {
            Loaded += (s, e) => UpdatePlaceholderVisibility();
            TextChanged += (s, e) => UpdatePlaceholderVisibility();
        }

        private void UpdatePlaceholderVisibility()
        {
            if (string.IsNullOrEmpty(Text))
                Foreground = System.Windows.SystemColors.GrayTextBrush;
            else
                Foreground = System.Windows.SystemColors.ControlTextBrush;
        }
    }
}
