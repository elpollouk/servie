using System;
using System.IO;
using System.Windows.Forms;

namespace Servie
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();

            DoubleBuffered = true;
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Force each service to stop
            foreach (TabPage tab in tabControl1.Controls)
            {
                if (tab is ConsoleTab)
                {
                    ConsoleTab ctab = tab as ConsoleTab;
                    ctab.Service.Stop();
                }
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {
            // Get a list of all the services in the environment and try to load them
            foreach (string dir in Directory.EnumerateDirectories("servers"))
            {
                try
                {
                    ServiceDetails.Service service = new ServiceDetails.Service(Path.GetFileName(dir));

                    ConsoleTab console = new ConsoleTab(service);
                    tabControl1.Controls.Add(console);

                    if (service.Autostart)
                    {
                        tabControl1.SelectedTab = console;
                        service.Start();
                    }
                }
                catch (ServiceDetails.IgnoreServiceException)
                {
                }
                catch (ServiceDetails.ParserError x)
                {
                    MessageBox.Show(x.Message, "Failed to load " + Path.GetFileName(dir), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
