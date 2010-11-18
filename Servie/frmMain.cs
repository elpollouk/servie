using System;
using System.IO;
using System.Windows.Forms;

using Servie.ServiceDetails;

namespace Servie
{
    public partial class frmMain : Form
    {
        private const int kExitTimeoutTime = 30000;
        private int m_ExitTimeout;
        private bool m_HideOnClose = false;

        private frmAbout m_About;

        public frmMain()
        {
            InitializeComponent();

            m_About = new frmAbout();

            DoubleBuffered = true;
        }

        private void Main_Load(object sender, EventArgs e)
        {
            tabControl1.Controls.Clear();

            m_ExitTimeout = kExitTimeoutTime / timerClosing.Interval;

            // Create a tab for each service
            foreach (Service service in ServiceLoader.Services)
            {
                ConsoleTab console = new ConsoleTab(service);
                //UserControlTab tab = new UserControlTab(console);
                tabControl1.Controls.Add(console);
            }
           
            // Auto start the services
            ServiceLoader.AutoStartServices(OnAutoStartComplete, DisplayServiceError);

            m_HideOnClose = true;
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (m_HideOnClose == true)
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                m_About.Dispose();
            }
        }

        void Exit()
        {
            if (!ServiceLoader.AreAllServicesStopped())
            {
                if (timerClosing.Enabled == false)
                {
                    ServiceLoader.StopAllServices();
                    timerClosing.Enabled = true;
                }
            }
            else
            {
                m_HideOnClose = false;
                Close();
            }
        }

        private void timerClosing_Tick(object sender, EventArgs e)
        {
            m_ExitTimeout--;
            if (ServiceLoader.AreAllServicesStopped())
            {
                timerClosing.Enabled = false;
                m_HideOnClose = false;
                this.Close();
            }
            else if (m_ExitTimeout == 0)
            {
                // Timed out while exiting
                DialogResult result = MessageBox.Show("Shutdown is taking a long time, do you want to continue waiting?", "Servie", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    m_ExitTimeout = kExitTimeoutTime / timerClosing.Interval;
                }
                else
                {
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
            trayIcon.BalloonTipTitle = "Servie";
            if (ServiceLoader.AreAllAutoStartServicesRunning())
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
            Show();
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }
            Activate();
        }

        #region Scheduled invoke code
        // This is a special interface to allow the scheduling of an event to be invoked at a later point
        // Only one event can be schedule at a time though.
        private EventHandler m_SIEvent = null;
        private object m_SISender = null;
        private EventArgs m_SIArgs = null;

        public void ScheduledInvoke(EventHandler evnt, object sender, EventArgs args, int delay)
        {
            if (m_SIEvent != null) throw new Exception("An event is already scheduled.");
            if (evnt == null) throw new ArgumentNullException();

            m_SIEvent = evnt;
            m_SISender = sender;
            m_SIArgs = args;

            timerScheduledInvoke.Interval = delay;
            timerScheduledInvoke.Enabled = true;
        }

        private void timerScheduledInvoke_Tick(object sender, EventArgs e)
        {
            timerScheduledInvoke.Enabled = false;

            EventHandler eventHandler = m_SIEvent;
            sender = m_SISender;
            e = m_SIArgs;
            // Xlear everything out so a new event can be raised
            m_SIEvent = null;
            m_SISender = null;
            m_SIArgs = null;

            // Trigger the event using the cached values
            eventHandler(sender, e);
        }
        #endregion

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
            if (!ServiceLoader.AreAllAutoStartServicesRunning())
            {
                ServiceLoader.AutoStartServices(OnAutoStartComplete, DisplayServiceError);
            }
        }

        private void stopAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!ServiceLoader.AreAllServicesStopped())
            {
                ServiceLoader.StopAllServices();
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_About.Show();
        }
    }
}
