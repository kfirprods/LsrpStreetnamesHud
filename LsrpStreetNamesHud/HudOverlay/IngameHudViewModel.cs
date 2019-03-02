using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Timers;
using DX9OverlayAPIWrapper;
using GtaSampApi;
using NonInvasiveKeyboardHookLibrary;
using ProcessesWatchdog;

namespace LsrpStreetNamesHud.HudOverlay
{
    // TODO: This class shouldn't have all the logic contained inside of it. Split it up!
    public class IngameHudViewModel : INotifyPropertyChanged
    {
        // Key codes for moving the HUD around
        private const int Numpad2 = 0x62, Numpad4 = 0x64, Numpad6 = 0x66, Numpad8 = 0x68, KeyM = 0x4D, NumpadPlus = 0x6B, NumpadMinus = 0x6D;
        private const int KeyH = 0x48;

        // This file will be created in %AppData% to persist user configuration
        private const string HudPreferencesFileName = "LSRP Street Names HUD preferences.json";

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
                if (this._isLimitedToVehicles == value) return;

                this._isLimitedToVehicles = value;
                this._hudPreferences.OnlyVisibleInVehicles = value;
                this._hudPreferences.Save();
                this.OnPropertyChanged();
            }
        }

        private readonly KeyboardHookManager _keyboardHookManager;
        private bool _isHudMovingEnabled;
        private TextLabel _hudText;
        private readonly object _updateTimerLock = new object();
        
        private readonly HudPreferences _hudPreferences;

        public IngameHudViewModel()
        {
            var hudPreferencesFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                HudPreferencesFileName);

            this._hudPreferences = HudPreferences.Load(hudPreferencesFilePath);
            this.IsLimitedToVehicles = this._hudPreferences.OnlyVisibleInVehicles;

            var updateHudTimer = new Timer(500);
            updateHudTimer.Elapsed += UpdateHudTimer_Elapsed;
            
            // The process watch dog will help us start/stop the update timer when the user goes in/out of the game
            var watchdog = new ProcessWatchdog("gta_sa");
            watchdog.OnProcessOpened += pid =>
            {
                SampApi.GtaProcessId = pid;
                updateHudTimer.Start();
            };
            watchdog.OnProcessClosed += () =>
            {
                this._keyboardHookManager.Stop();

                SampApi.GtaProcessId = null;
                updateHudTimer.Stop();
                this._hudText = null;
            };
            watchdog.Start();

            // TODO: Use project-wide singleton to make sure no more than 1 keyboard hook manager is ever instantiated
            this._keyboardHookManager = new KeyboardHookManager();
            this._keyboardHookManager.Start();

            // TODO: Move the mover logic elsewhere
            #region HUD Mover
            // Key combination for toggling the HUD mover
            this._keyboardHookManager.RegisterHotkey(ModifierKeys.Alt, KeyM, () =>
            {
                this._isHudMovingEnabled = !this._isHudMovingEnabled;
                
                if (!this._isHudMovingEnabled)
                {
                    this._hudPreferences.Save();
                    SampApi.AddMessageToChat("{FFFFFF}[{FF0000}HUD{FFFFFF}] Disabled HUD mover and saved your settings");
                }
                else
                {
                    SampApi.AddMessageToChat(
                        "{FFFFFF}[{FF0000}HUD{FFFFFF}] HUD mover enabled. Use NumPad arrows (hold Alt to boost) for relocating NumPad +/- for resizing");
                    SampApi.AddMessageToChat(
                        "{FFFFFF}[{FF0000}HUD{FFFFFF}] (!) To save your changes you must disable the HUD mover with Alt+M");
                }
            });

            // Keys for resizing the HUD with the mover
            this._keyboardHookManager.RegisterHotkey(NumpadPlus, () => { this.ResizeHudText(1); });
            this._keyboardHookManager.RegisterHotkey(NumpadMinus, () => { this.ResizeHudText(-1); });

            // Keys for moving the HUD with the mover
            this._keyboardHookManager.RegisterHotkey(Numpad2, () =>
            {
                this.MoveHudText(0, 1);
            });

            this._keyboardHookManager.RegisterHotkey(ModifierKeys.Alt, Numpad2, () =>
            {
                this.MoveHudText(0, 10);
            });

            this._keyboardHookManager.RegisterHotkey(Numpad6, () =>
            {
                this.MoveHudText(1, 0);
            });

            this._keyboardHookManager.RegisterHotkey(ModifierKeys.Alt, Numpad6, () =>
            {
                this.MoveHudText(10, 0);
            });

            this._keyboardHookManager.RegisterHotkey(Numpad8, () =>
            {
                this.MoveHudText(0, -1);
            });

            this._keyboardHookManager.RegisterHotkey(ModifierKeys.Alt, Numpad8, () =>
            {
                this.MoveHudText(0, -10);
            });

            this._keyboardHookManager.RegisterHotkey(Numpad4, () =>
            {
                this.MoveHudText(-1, 0);
            });

            this._keyboardHookManager.RegisterHotkey(ModifierKeys.Alt, Numpad4, () =>
            {
                this.MoveHudText(-10, 0);
            });
            #endregion

            // Keyboard shortcut for toggling the HUD
            this._keyboardHookManager.RegisterHotkey(ModifierKeys.Alt, KeyH, () =>
            {
                this.IsEnabled = !this.IsEnabled;
            });
        }

        private void MoveHudText(int shiftX, int shiftY)
        {
            if (!this._isHudMovingEnabled) return;
            if (this._hudText == null) return;
            if (!this._hudText.IsVisible) return;

            this._hudText.Position = new Point(this._hudText.Position.X + shiftX, this._hudText.Position.Y + shiftY);

            this._hudPreferences.X = this._hudText.Position.X;
            this._hudPreferences.Y = this._hudText.Position.Y;
        }

        private void ResizeHudText(int fontShift)
        {
            if (!this._isHudMovingEnabled) return;
            if (this._hudText == null) return;
            if (!this._hudText.IsVisible) return;

            this._hudText.FontSize += fontShift;
            this._hudPreferences.FontSize = this._hudText.FontSize;
        }

        public void Destroy()
        {
            this._hudText?.Destroy();
        }

        private void UpdateHudTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (this._updateTimerLock)
            {
                #region Check for reasons to hide the HUD
                // Show nothing if the player disabled the HUD
                if (!this.IsEnabled ||
                    // Show nothing if the player is in an interior
                    SampApi.IsPlayerInAnyInterior() ||
                    // Show nothing if the player is in the escape menu
                    SampApi.IsPlayerInEscapeMenu() ||
                    // Show nothing if the player is on foot and had limited the HUD to show only when they're in vehicles
                    this.IsLimitedToVehicles && !SampApi.IsPlayerInAnyVehicle())
                {
                    if (this._hudText != null)
                        this._hudText.IsVisible = false;
                    return;
                }
                #endregion

                var coordinates = SampApi.GetPlayerCoordinates();
                if (!coordinates.HasValue) return;

                // Prevent premature rendering (i.e. user hasn't even connected to the server yet)
                // * Premature rendering could cause crashes
                if (coordinates.Value.X == 0 && coordinates.Value.Y == 0 && coordinates.Value.Z == 0) return;

                // Get direction (N/W/S/E)
                var currentFacingAngle = SampApi.GetPlayerFacingAngle();

                // Even more premature rendering prevention - this is the facing angle when you're in the LS-RP login screen
                if (Math.Abs(currentFacingAngle - (-98)) < 0.0001)
                {
                    return;
                }

                var currentDirection = FacingAngleToDirection(currentFacingAngle);

                // Get zone (e.g. Rodeo, Commerce...)
                var playerZone = SampApi.GetPlayerCurrentZone();

                // Debug.WriteLine($"{currentFacingAngle} - {playerZone}");

                // Initialize overlay text label if necessary
                if (this._hudText == null)
                {
                    this._hudText = new TextLabel("Arial", this._hudPreferences.FontSize, this._hudPreferences.X, this._hudPreferences.Y,
                        Color.DimGray, "Doakes HUD");
                }

                this._hudText.IsVisible = true;

                this._hudText.Text = $"|{currentDirection}| {playerZone}"; // e.g. |S| Rodeo
            }
        }

        // TODO: Move to a utility module
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

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
