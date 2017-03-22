using System;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Windows;
using Console.Util;

namespace Console
{
    /// <summary>
    ///     Control.xaml 的交互逻辑
    /// </summary>
    public partial class Control : Window
    {
        private static readonly string SourcePath = Environment.CurrentDirectory + "\\AnwConnector";

        private static readonly string DestPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) +
                                                  "\\AnwConnector";

        public Control()
        {
            InitializeComponent();
        }

        private void Installer_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(DestPath))
            {
                Directory.CreateDirectory(DestPath);
            }
            if (File.Exists(DestPath + "\\AnwConnector.exe"))
            {
                File.Delete(DestPath + "\\AnwConnector.exe");
                ConfigHelper.UpdateAppConfig("AnwConnector", DestPath + "\\AnwConnector.exe");
            }
            File.Copy(SourcePath + "\\AnwConnector.exe", DestPath + "\\AnwConnector.exe");

            if (!File.Exists(DestPath + "\\AnwConnector.exe.config"))
            {
                File.Copy(SourcePath + "\\AnwConnector.exe.config", DestPath + "\\AnwConnector.exe.config");
            }
            if (!File.Exists(DestPath + "\\Newtonsoft.Json.dll"))
            {
                File.Copy(SourcePath + "\\Newtonsoft.Json.dll", DestPath + "\\Newtonsoft.Json.dll");
            }
            if (!File.Exists(DestPath + "\\log4net.dll"))
            {
                File.Copy(SourcePath + "\\log4net.dll", DestPath + "\\log4net.dll");
            }
            if (!File.Exists(DestPath + "\\log4net.xml"))
            {
                File.Copy(SourcePath + "\\log4net.xml", DestPath + "\\log4net.xml");
            }
            string[] args = {ConfigHelper.GetAppConfig("AnwConnector")};
            if (!ServiceIsExisted("AnwConnector"))
            {
                try
                {
                    ManagedInstallerClass.InstallHelper(args);
                    MessageBox.Show("安装完毕");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                MessageBox.Show("该服务已经存在，不用重复安装。");
            }
        }

        private void UnInstaller_Click(object sender, RoutedEventArgs e)
        {
            string[] args = {"/u", ConfigHelper.GetAppConfig("AnwConnector")};
            if (ServiceIsExisted("AnwConnector"))
            {
                try
                {
                    ManagedInstallerClass.InstallHelper(args);
                    MessageBox.Show("卸载完毕");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                MessageBox.Show("该服务不存在，不用卸载。");
            }
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (ServiceIsExisted("AnwConnector"))
            {
                var serviceController = new ServiceController("AnwConnector");
                if (serviceController.Status == ServiceControllerStatus.Stopped)
                {
                    try
                    {
                        string[] args = {"", ""};
                        serviceController.Start(args);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (ServiceIsExisted("AnwConnector"))
            {
                var serviceController = new ServiceController("AnwConnector");
                if (serviceController.Status == ServiceControllerStatus.Running)
                {
                    try
                    {
                        serviceController.Stop();
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
        }

        private bool ServiceIsExisted(string svcName)
        {
            var services = ServiceController.GetServices();
            return services.Any(s => s.ServiceName == svcName);
        }
    }
}