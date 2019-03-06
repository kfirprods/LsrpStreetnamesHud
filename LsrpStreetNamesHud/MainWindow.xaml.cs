using System;
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
        private static readonly log4net.ILog Logger =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public MainWindow()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            InitializeComponent();

            this.DataContext = new IngameHudViewModel();

            Dx9Overlay.SetParam("use_window", "1"); // Use the window name to find the process
            Dx9Overlay.SetParam("window", "GTA:SA:MP");
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Info("Unhandled exception", (Exception)e.ExceptionObject);
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (!(this.DataContext is IngameHudViewModel viewModel)) return;
            viewModel.Destroy();
        }
    }
}
