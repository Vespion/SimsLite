using System;
using Windows.ApplicationModel.Resources;
using Windows.System;
using Windows.UI.Xaml;
using JetBrains.Annotations;

namespace SimsLite_Parents.Helpers
{
    internal static class ResourceExtensions
    {
        private static ResourceLoader _resLoader = new ResourceLoader();

        public static string GetLocalized(this string resourceKey)
        {
            return _resLoader.GetString(resourceKey);
        }

        [CanBeNull]
        public static User GetLauncher(this Application application)
        {
            if (application is App app)
            {
                return app.LaunchedBy;
            }

            return null;
        }

        public static IServiceProvider GetProvider(this Application application)
        {
            if (application is App app)
            {
                return app.Services;
            }

            return null;
        }
    }
}
