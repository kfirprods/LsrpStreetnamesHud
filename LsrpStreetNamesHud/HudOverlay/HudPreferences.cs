using System.IO;
using Newtonsoft.Json;

namespace LsrpStreetNamesHud.HudOverlay
{
    public class HudPreferences
    {
        private static readonly object _hudPreferencesFileLock = new object();

        private static readonly log4net.ILog Logger =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        #region Properties
        // The default X, Y values will place the hud below the mini-map for most players
        [JsonProperty]
        public int X { get; set; } = 60;

        [JsonProperty]
        public int Y { get; set; } = 570;

        [JsonProperty]
        public int FontSize { get; set; } = 12;

        [JsonProperty]
        public bool OnlyVisibleInVehicles { get; set; }
        #endregion

        [JsonIgnore]
        public string FilePath { get; set; }

        #region API
        public static HudPreferences Load(string hudPreferencesFilePath)
        {
            Logger.Info($"Loading hud preferences from {hudPreferencesFilePath}");

            // If the file does not exist, return default preferences
            if (!File.Exists(hudPreferencesFilePath))
            {
                Logger.Info("Hud preferences file not found. Returning default preferences");
                return new HudPreferences() { FilePath = hudPreferencesFilePath };
            }

            lock (_hudPreferencesFileLock)
            {
                var serializedPreferences = File.ReadAllText(hudPreferencesFilePath);
                var preferences = JsonConvert.DeserializeObject<HudPreferences>(serializedPreferences);
                preferences.FilePath = hudPreferencesFilePath;

                Logger.Info("Hud preferences file loaded successfully");

                return preferences;
            }
        }

        public void Save()
        {
            Logger.Info($"Saving hud preferences to {this.FilePath}");

            lock (_hudPreferencesFileLock)
            {
                var serializedPreferences = JsonConvert.SerializeObject(this);
                File.WriteAllText(this.FilePath, serializedPreferences);
            }

            Logger.Info("Successfully saved hud preferences");
        }
        #endregion
    }
}
