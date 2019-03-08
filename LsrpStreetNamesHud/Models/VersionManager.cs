using System.Linq;
using System.Net;
using System.Windows;
using LsrpStreetNamesHud.Common.Extensions;
using Newtonsoft.Json;

namespace LsrpStreetNamesHud.Models
{
    public class VersionManager
    {
        private const string RemoteVersionsFile = "https://raw.githubusercontent.com/kfirprods/LsrpStreetnamesHud/master/versions.json";

        private static VersionManager _instance;

        public static VersionManager Instance => _instance ?? (_instance = new VersionManager());

        private VersionInfo[] _versions;
        public VersionInfo[] Versions => this._versions ?? (this._versions = FetchVersions());

        public VersionInfo GetCurrentVersion()
        {
            return this.Versions.First(version => version.Version == Application.Current.GetVersion());
        }

        public VersionInfo GetLatestVersion()
        {
            return this.Versions.First(version => this.Versions.All(otherVersion => version.Version >= otherVersion.Version));
        }

        public bool IsUpToDate()
        {
            return this.GetCurrentVersion().Version == GetLatestVersion().Version;
        }

        private static VersionInfo[] FetchVersions()
        {
            var webClient = new WebClient();
            var rawVersions = webClient.DownloadString(RemoteVersionsFile);
            return JsonConvert.DeserializeObject<VersionInfo[]>(rawVersions);
        }
    }

    
}
