namespace Leviathan.Core.Localization
{
    public static class LocalizationHelper
    {
        public static string GetLocalizedString(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException(name);
            
            var localized = Localization.ResourceManager.GetString(name);

            if (string.IsNullOrEmpty(localized)) throw new KeyNotFoundException(localized);

            return localized;
        }
    }
}