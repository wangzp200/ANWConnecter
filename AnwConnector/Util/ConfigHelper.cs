using System;
using System.Configuration;
using System.Linq;
using System.Xml;

namespace AnwConnector.Util
{
    public static class ConfigHelper
    {
        private static XmlDocument xmlDocument=new XmlDocument();

        static ConfigHelper()
        {
            xmlDocument.Load(AppDomain.CurrentDomain.BaseDirectory + "AnwConnector.exe.config");
        }

        public static string GetAppConfig(string strKey)
        {

            return "";
        }

        public static void UpdateAppConfig(string newKey, string newValue)
        {
            //ConfigurationManager.OpenExeConfiguration(AppDomain.CurrentDomain.BaseDirectory + "AnwConnector.exe.config");
            //var isModified = false;
            //foreach (string key in ConfigurationManager.AppSettings)
            //{
            //    if (key == newKey)
            //    {
            //        isModified = true;
            //    }
            //}

            //// Open App.Config of executable  
            //var config =
            //    ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            //// You need to remove the old settings object before you can replace it  
            //if (isModified)
            //{
            //    config.AppSettings.Settings.Remove(newKey);
            //}
            //// Add an Application Setting.  
            //config.AppSettings.Settings.Add(newKey, newValue);
            //// Save the changes in App.config file.  
            //config.Save(ConfigurationSaveMode.Modified);
            //// Force a reload of a changed section.  
            //ConfigurationManager.RefreshSection("appSettings");
        }
    }
}