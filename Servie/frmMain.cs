using System;
using System.IO;
using System.Windows.Forms;

using Servie.ServiceDetails;

namespace Servie
{
    public partial class frmMain : Form
    {
        private const int kExitTimeoutTime = 30000; // Time to wait for services to finish when exiting

        private int m_ExitTimeoutCounter;
        private bool m_HideOnClose = false;
        private bool m_FirstAutoStart = true;

        private frmAbout m_About;

        public frmMain()
        {
            InitializeComponent();

            m_About = new frmAbout();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            tabControl1.Controls.Clear();

            // Initialise the time out counter based on the form's timer interval
            m_ExitTimeoutCounter = kExitTimeoutTime / timerClosing.Interval;

            // Create a tab for each service
            foreach (Service service in ServiceLoader.Services)
            {
                ConsoleTab console = new ConsoleTab(service);
                tabControl1.Controls.Add(console);
            }
           
            // Auto start the services
            ServiceLoader.Started += OnAutoStartComplete;
            ServiceLoader.AutoStartServices(DisplayServiceError);

            m_HideOnClose = true;
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (m_HideOnClose == true)
            {
                // We're not exiting, so we only want to hide this window
                e.Cancel = true;
                Hide();
            }
            else
            {
                // We are exiting, so brute force close the about window (this is because it also hides on close)
                m_About.Dispose();
            }
        }

        // Starts the exit procedure
        private void Exit()
        {
            if (!ServiceLoader.AreAllServicesStopped)
            {
                // We need to request that all services are stopped before we can exit
                if (timerClosing.Enabled == false)
                {
                    ServiceLoader.StopAllServices();
                    timerClosing.Enabled = true;
                }
            }
            else
            {
                // No services are running so we can just close this window
                m_HideOnClose = false;
                Close();
            }
        }

        private void timerClosing_Tick(object sender, EventArgs e)
        {
            m_ExitTimeoutCounter--;
            if (ServiceLoader.AreAllServicesStopped)
            {
                // Everything has been stopped, so we can close the form now
                timerClosing.Enabled = false;
                m_HideOnClose = false;
                this.Close();
            }
            else if (m_ExitTimeoutCounter == 0)
            {
                // Timed out while exiting
                DialogResult result = MessageBox.Show("Shutdown is taking a long time, do you want to continue waiting?", "Servie", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    m_ExitTimeoutCounter = kExitTimeoutTime / timerClosing.Interval;
                }
                else
                {
                    // Force kill all the services
                    foreach (Service service in ServiceLoader.Services)
                    {
                        if (service.IsRunning)
                        {
                            service.Kill();
                        }
                    }
                }
            }
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case Program.WM_USER:
                    // A new instance is trying to activate us
                    ReDisplayWindow();
                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        private void OnAutoStartComplete(object sender, EventArgs e)
        {
            // We only want to display the notification if we don't have focus or on the first time we're started
            if (!m_FirstAutoStart && (ActiveForm == this)) return;
            m_FirstAutoStart = false;

            trayIcon.BalloonTipTitle = "Servie";
            trayIcon.BalloonTipIcon = ToolTipIcon.Info;
            if (ServiceLoader.AreAllAutoStartServicesRunning)
            {
                trayIcon.BalloonTipText = "All services started successfully.";
            }
            else
            {
                trayIcon.BalloonTipText = "Failed to start some services.";
            }
            trayIcon.ShowBalloonTip(10000);
        }

        public void DisplayServiceError(string service, string message)
        {
            MessageBox.Show(message, service, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ReDisplayWindow()
        {
            // Show the window, and bring it to the front
            Show();
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }
            Activate();
        }

        #region Tray icon code
        //---------------------------------------------------------------------------------------//
        // Context Menu
        //---------------------------------------------------------------------------------------//
        private void trayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                ReDisplayWindow();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Exit();
        }

        private void startAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!ServiceLoader.AreAllAutoStartServicesRunning && !ServiceLoader.IsStartingService)
            {
                ServiceLoader.AutoStartServices(DisplayServiceError);
            }
        }

        private void stopAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!ServiceLoader.AreAllServicesStopped)
            {
                ServiceLoader.StopAllServices();
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_About.Show();
        }
        #endregion
    }
}
