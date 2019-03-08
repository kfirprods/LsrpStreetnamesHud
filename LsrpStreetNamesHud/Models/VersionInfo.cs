using System;
using Newtonsoft.Json;

namespace LsrpStreetNamesHud.Models
{
    public class VersionInfo
    {
        [JsonProperty("version_number")]
        public string VersionNumber { get; set; }

        [JsonProperty("download_link")]
        public string DownloadLink { get; set; }

        [JsonProperty("release_notes")]
        public string ReleaseNotes { get; set; }

        [JsonIgnore]
        public Version Version => new Version(this.VersionNumber);
    }
}
