using System;
using System.ComponentModel;
using System.Windows;
using DX9OverlayAPIWrapper;
using LsrpStreetNamesHud.ViewModels;

namespace LsrpStreetNamesHud
{
    public partial class MainWindow
    {
        private static readonly log4net.ILog Logger =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly System.Windows.Forms.NotifyIcon _notifyIcon;

        public MainWindow()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            InitializeComponent();

            this.DataContext = new MainWindowViewModel();

            Dx9Overlay.SetParam("use_window", "1"); // Use the window name to find the process
            Dx9Overlay.SetParam("window", "GTA:SA:MP");

            #region Create system tray notification icon
            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = Properties.Resources.map_icon,
                Visible = true,
                Text = "Double click me to open the program!"
            };
            _notifyIcon.DoubleClick +=
                delegate
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                };
            var notificationIconContextMenu = new System.Windows.Forms.ContextMenu();
            notificationIconContextMenu.MenuItems.Add("Exit", delegate { this.Close(); });
            _notifyIcon.ContextMenu = notificationIconContextMenu;
            #endregion
        }

        private bool _wasErrorMessageShown;

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Info("Unhandled exception", (Exception)e.ExceptionObject);

            if (!this._wasErrorMessageShown)
            {
                this._wasErrorMessageShown = true;
                MessageBox.Show(
                    "An unexpected error occured. Its details are written to a HUD.log file, be a sport and send it to the developer\nThe application will now exit, you can restart it to try again");
            }

            this.Close();
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (!(this.DataContext is IngameHudViewModel viewModel)) return;
            viewModel.Destroy();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            var viewModel = (MainWindowViewModel) this.DataContext;

            if (viewModel.ShouldMinimizeToTray && this.WindowState == WindowState.Minimized)
            {
                this.Hide();
            }

            base.OnStateChanged(e);
        }
    }
}
