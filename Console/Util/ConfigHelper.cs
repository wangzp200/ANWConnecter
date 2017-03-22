using System.Configuration;
using System.Linq;

namespace Console.Util
{
    public static class ConfigHelper
    {
        public static string GetAppConfig(string strKey)
        {
            if (ConfigurationManager.AppSettings.Cast<string>().Any(key => key == strKey))
            {
                return ConfigurationManager.AppSettings[strKey];
            }
            return null;
        }

        public static void UpdateAppConfig(string newKey, string newValue)
        {
            var isModified = false;
            foreach (string key in ConfigurationManager.AppSettings)
            {
                if (key == newKey)
                {
                    isModified = true;
                }
            }

            // Open App.Config of executable  
            var config =
                ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            // You need to remove the old settings object before you can replace it  
            if (isModified)
            {
                config.AppSettings.Settings.Remove(newKey);
            }
            // Add an Application Setting.  
            config.AppSettings.Settings.Add(newKey, newValue);
            // Save the changes in App.config file.  
            config.Save(ConfigurationSaveMode.Modified);
            // Force a reload of a changed section.  
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}