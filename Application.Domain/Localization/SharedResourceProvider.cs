using System.Globalization;
using System.Resources;

namespace Application.Domain.Localization
{
    /// <summary>
    /// Lightweight accessor for SharedResource strings without DI (for static contexts).
    /// </summary>
    public static class SharedResourceProvider
    {
        private static readonly ResourceManager ResourceManager =
            new ResourceManager("Application.Domain.Resources.SharedResource", typeof(SharedResource).Assembly);

        public static string GetString(string key, params object[]? arguments)
        {
            var value = ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
            return (arguments?.Length ?? 0) > 0 ? string.Format(value, arguments!) : value;
        }
    }
}
