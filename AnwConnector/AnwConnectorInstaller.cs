using System.ComponentModel;
using System.Configuration.Install;

namespace AnwConnector
{
    [RunInstaller(true)]
    public partial class AnwConnectorInstaller : Installer
    {
        public AnwConnectorInstaller()
        {
            InitializeComponent();
        }
    }
}