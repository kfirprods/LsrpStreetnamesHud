using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using LsrpStreetNamesHud.Common.Extensions;
using LsrpStreetNamesHud.Models;


namespace LsrpStreetNamesHud.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        // This file will be created in %AppData% to persist user configuration
        private const string AppPreferencesFileName = "LSRP Street Names App preferences.json";

        private bool _shouldStartWithWindows;

        public bool ShouldStartWithWindows
        {
            get => this._shouldStartWithWindows;
            set
            {
                this._shouldStartWithWindows = value;
                this.OnPropertyChanged();

                if (value)
                {
                    Assembly.GetExecutingAssembly().CreateStartupShortcut();
                }
                else
                {
                    Assembly.GetExecutingAssembly().RemoveStartupShortcut();
                }
            }
        }

        private bool _shouldMinimizeToTray;

        public bool ShouldMinimizeToTray
        {
            get => this._shouldMinimizeToTray;
            set
            {
                this._shouldMinimizeToTray = value;
                this.OnPropertyChanged();

                this._applicationPreferences.ShouldMinimizeToTray = value;
                this._applicationPreferences.Save();
            }
        }

        public IngameHudViewModel HudViewModel { get; } = new IngameHudViewModel();

        private readonly ApplicationPreferences _applicationPreferences;

        public MainWindowViewModel()
        {
            this.ShouldStartWithWindows = Assembly.GetExecutingAssembly().IsInStartupFolder();

            var appPreferencesFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppPreferencesFileName);

            this._applicationPreferences = ApplicationPreferences.Load(appPreferencesFilePath);
            this.ShouldMinimizeToTray = this._applicationPreferences.ShouldMinimizeToTray;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
