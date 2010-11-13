using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Servie
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string catalinaHome = "servers\\apache-tomcat-6.0.29";
            string tomcat = catalinaHome + "\\bin\\catalina.bat";

            Dictionary<string, string> env = new Dictionary<string,string>();
            env.Add("CATALINA_HOME", catalinaHome);
            env.Add("JAVA_HOME", "C:\\Program Files\\Java\\jdk1.6.0_22");

            ConsoleTab service = new ConsoleTab(tomcat, "start", env);
            tabControl1.Controls.Add(service);
            tabControl1.SelectedTab = service;

            service.Start();
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (TabPage tab in tabControl1.Controls)
            {
                if (tab is ConsoleTab)
                {
                    ConsoleTab ctab = tab as ConsoleTab;
                    ctab.Stop();
                }
            }
        }
    }
}
