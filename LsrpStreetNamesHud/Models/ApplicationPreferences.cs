using System.IO;
using Newtonsoft.Json;

namespace LsrpStreetNamesHud.Models
{
    public class ApplicationPreferences
    {
        private static readonly object ApplicationPreferencesLock = new object();

        private static readonly log4net.ILog Logger =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region Properties
        [JsonProperty]
        public bool ShouldMinimizeToTray { get; set; }
        #endregion

        [JsonIgnore]
        public string FilePath { get; set; }

        #region API
        public static ApplicationPreferences Load(string appPreferencesFilePath)
        {
            Logger.Info($"Loading app preferences from {appPreferencesFilePath}");

            // If the file does not exist, return default preferences
            if (!File.Exists(appPreferencesFilePath))
            {
                Logger.Info("App preferences file not found. Returning default preferences");
                return new ApplicationPreferences() { FilePath = appPreferencesFilePath };
            }

            lock (ApplicationPreferencesLock)
            {
                var serializedPreferences = File.ReadAllText(appPreferencesFilePath);
                var preferences = JsonConvert.DeserializeObject<ApplicationPreferences>(serializedPreferences);
                preferences.FilePath = appPreferencesFilePath;

                Logger.Info("App preferences file loaded successfully");

                return preferences;
            }
        }

        public void Save()
        {
            Logger.Info($"Saving app preferences to {this.FilePath}");

            lock (ApplicationPreferencesLock)
            {
                var serializedPreferences = JsonConvert.SerializeObject(this);
                File.WriteAllText(this.FilePath, serializedPreferences);
            }

            Logger.Info("Successfully saved app preferences");
        }
        #endregion
    }
}
