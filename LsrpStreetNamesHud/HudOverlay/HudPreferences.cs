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
        #endregion

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
                return JsonConvert.DeserializeObject<HudPreferences>(serializedPreferences);
            }
        }

        public void Save(string hudPreferencesFilePath)
        {
            lock (_hudPreferencesFileLock)
            {
                var serializedPreferences = JsonConvert.SerializeObject(this);
                File.WriteAllText(hudPreferencesFilePath, serializedPreferences);
            }
        }
        #endregion
    }
}
