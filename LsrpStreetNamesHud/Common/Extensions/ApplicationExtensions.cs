using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace LsrpStreetNamesHud.Common.Extensions
{
    public static class ApplicationExtensions
    {
        public static Version GetVersion(this Application app)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

            return Version.Parse(fileVersionInfo.ProductVersion);
        }
    }
}
