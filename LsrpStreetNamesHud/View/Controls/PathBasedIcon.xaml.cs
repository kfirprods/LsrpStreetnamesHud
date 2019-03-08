using System.Windows;

namespace LsrpStreetNamesHud.UI
{
    public partial class PathBasedIcon
    {
        public static DependencyProperty IconDataProperty =
            DependencyProperty.Register(nameof(IconData), typeof(string), typeof(PathBasedIcon));

        public string IconData
        {
            get => (string) this.GetValue(IconDataProperty);
            set => this.SetValue(IconDataProperty, value);
        }

        public PathBasedIcon()
        {
            InitializeComponent();
        }
    }
}
