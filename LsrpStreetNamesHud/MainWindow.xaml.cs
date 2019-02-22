using System.ComponentModel;
using DX9OverlayAPIWrapper;
using LsrpStreetNamesHud.HudOverlay;

namespace LsrpStreetNamesHud
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = new IngameHudViewModel();

            Dx9Overlay.SetParam("use_window", "1"); // Use the window name to find the process
            Dx9Overlay.SetParam("window", "GTA:SA:MP");
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (!(this.DataContext is IngameHudViewModel viewModel)) return;
            viewModel.Destroy();
        }
    }
}
