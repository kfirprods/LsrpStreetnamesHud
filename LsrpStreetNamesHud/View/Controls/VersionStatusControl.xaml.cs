using System.Diagnostics;
using System.Windows.Navigation;
using LsrpStreetNamesHud.Models;

namespace LsrpStreetNamesHud.View.Controls
{
    public partial class VersionStatusControl
    {
        public bool IsUpToDate => VersionManager.Instance.IsUpToDate();

        public VersionInfo LatestVersion => VersionManager.Instance.GetLatestVersion();

        public VersionStatusControl()
        {
            InitializeComponent();
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
