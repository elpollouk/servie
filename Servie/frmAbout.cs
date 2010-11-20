using System;
using System.Windows.Forms;
using Servie.ServiceDetails;

namespace Servie
{
    public partial class frmAbout : Form
    {
        private const string kHomePageUrl = "http://code.google.com/p/servie/";
        private const string kLicenseUrl = "http://www.apache.org/licenses/LICENSE-2.0.html";
        public frmAbout()
        {
            InitializeComponent();
        }

        private void frmAbout_Load(object sender, EventArgs e)
        {
            // Get the actual assembly version rather than hard code this in.
            lblServie.Text = "Servie " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private void cmdOk_Click(object sender, EventArgs e)
        {
            Hide();
        }

        private void frmAbout_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Closing the window doesn't actually destroy it
            e.Cancel = true;
            Hide();
        }

        private void frmAbout_VisibleChanged(object sender, EventArgs e)
        {
            if (Visible)
            {
                // If we've become visible, start the update timer for the number of running services
                UpdateNumRunning();
                timerUpdate.Enabled = true;
            }
            else
            {
                timerUpdate.Enabled = false;
            }
        }

        private void timerUpdate_Tick(object sender, EventArgs e)
        {
            UpdateNumRunning();
        }

        private void UpdateNumRunning()
        {
            lblNumRunning.Text = ServiceLoader.NumRunningServices + "/" + ServiceLoader.NumLoadedServices + " servers running.";
        }

        private void lnkLicense_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(kLicenseUrl);
        }

        private void lnkWebsite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(kHomePageUrl);
        }
    }
}
