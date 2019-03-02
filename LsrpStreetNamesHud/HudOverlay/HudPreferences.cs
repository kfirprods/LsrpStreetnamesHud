using System.IO;
using Newtonsoft.Json;

namespace LsrpStreetNamesHud.HudOverlay
{
    public class HudPreferences
    {
        private static readonly object _hudPreferencesFileLock = new object();

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
            // If the file does not exist, return default preferences
            if (!File.Exists(hudPreferencesFilePath))
            {
                return new HudPreferences();
            }

            lock (_hudPreferencesFileLock)
            {
                var serializedPreferences = File.ReadAllText(hudPreferencesFilePath);
                var preferences = JsonConvert.DeserializeObject<HudPreferences>(serializedPreferences);
                preferences.FilePath = hudPreferencesFilePath;
                return preferences;
            }
        }

        public void Save()
        {
            lock (_hudPreferencesFileLock)
            {
                var serializedPreferences = JsonConvert.SerializeObject(this);
                File.WriteAllText(this.FilePath, serializedPreferences);
            }
        }
        #endregion
    }
}
