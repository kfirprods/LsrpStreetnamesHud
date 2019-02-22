using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Timers;
using DX9OverlayAPIWrapper;
using LsrpStreetNamesHud.GtaApi;
using ProcessesWatchdog;

namespace LsrpStreetNamesHud.HudOverlay
{
    public class IngameHudViewModel : INotifyPropertyChanged
    {
        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => this._isEnabled;
            set
            {
                this._isEnabled = value;
                this.OnPropertyChanged();
            }
        }

        private bool _isLimitedToVehicles;

        public bool IsLimitedToVehicles
        {
            get => this._isLimitedToVehicles;
            set
            {
                this._isLimitedToVehicles = value;
                this.OnPropertyChanged();
            }
        }

        public IngameHudViewModel()
        {
            var updateHudTimer = new Timer(500);
            updateHudTimer.Elapsed += UpdateHudTimer_Elapsed;
            
            // The process watch dog will help us start/stop the update timer when the user goes in/out of the game
            var watchdog = new ProcessWatchdog("gta_sa");
            watchdog.OnProcessOpened += pid =>
            {
                UdfBasedApi.GtaProcessId = pid;
                updateHudTimer.Start();
            };
            watchdog.OnProcessClosed += () =>
            {
                UdfBasedApi.GtaProcessId = null;
                updateHudTimer.Stop();
                this._hudText = null;
            };
            watchdog.Start();
        }

        public void Destroy()
        {
            this._hudText?.Destroy();
        }

        private TextLabel _hudText;
        private readonly object _updateTimerLock = new object();

        private void UpdateHudTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (this._updateTimerLock)
            {
                #region Check for reasons to hide the HUD
                // Show nothing if the player disabled the HUD
                if (!this.IsEnabled ||
                    // Show nothing if the player is in an interior
                    UdfBasedApi.IsPlayerInAnyInterior() ||
                    // Show nothing if the player is in the escape menu
                    UdfBasedApi.IsPlayerInEscapeMenu() ||
                    // Show nothing if the player is on foot and had limited the HUD to show only when they're in vehicles
                    this.IsLimitedToVehicles && !UdfBasedApi.IsPlayerInAnyVehicle())
                {
                    if (this._hudText != null)
                        this._hudText.IsVisible = false;
                    return;
                }
                #endregion

                var coordinates = UdfBasedApi.GetPlayerCoordinates();
                if (!coordinates.HasValue) return;

                // Prevent premature rendering (i.e. user hasn't even connected to the server yet)
                // * Premature rendering could cause crashes
                if (coordinates.Value.X == 0 && coordinates.Value.Y == 0 && coordinates.Value.Z == 0) return;

                // Get direction (N/W/S/E)
                var currentFacingAngle = UdfBasedApi.GetPlayerFacingAngle();

                // Even more premature rendering prevention - this is the facing angle when you're in the LS-RP login screen
                if (Math.Abs(currentFacingAngle - (-98)) < 0.0001)
                {
                    return;
                }

                var currentDirection = FacingAngleToDirection(currentFacingAngle);

                // Get zone (e.g. Rodeo, Commerce...)
                var playerZone = UdfBasedApi.GetPlayerCurrentZone();

                Debug.WriteLine($"{currentFacingAngle} - {playerZone}");

                // Initialize overlay text label if necessary
                if (this._hudText == null)
                {
                    // (60, 570) places the label nicely below the map and works nicely on all resolutions
                    this._hudText = new TextLabel("Arial", 12, 60, 570,
                        Color.DimGray, "Doakes HUD");
                }

                this._hudText.IsVisible = true;

                this._hudText.Text = $"|{currentDirection}| {playerZone}"; // e.g. |S| Rodeo
            }
        }

        private static string FacingAngleToDirection(double angle)
        {
            if (angle >= -45 && angle < 45)
            {
                return "N";
            }
            else if (angle >= 45 && angle < 135)
            {
                return "W";
            }
            else if (angle >= 135 || angle <= -135)
            {
                return "S";
            }
            else
            {
                return "E";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
