using System;
using System.IO;
using System.Windows.Forms;

using Servie.ServiceDetails;

namespace Servie
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();

            DoubleBuffered = true;
        }

        private void Main_Load(object sender, EventArgs e)
        {
            tabControl1.Controls.Clear();

            ServiceLoader.LoadServices(DisplayServiceLoadError);
            foreach (Service service in ServiceLoader.Services)
            {
                ConsoleTab tab = new ConsoleTab(service);
                tabControl1.Controls.Add(tab);
            }

            ServiceLoader.AutoStartServices(OnAutoStartComplete, DisplayServiceLoadError);
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!ServiceLoader.AreAllServicesStopped())
            {
                if (timerClosing.Enabled == false)
                {
                    ServiceLoader.StopAllServices();
                    timerClosing.Enabled = true;
                }
                e.Cancel = true;
            }
        }

        private void timerClosing_Tick(object sender, EventArgs e)
        {
            if (ServiceLoader.AreAllServicesStopped())
            {
                timerClosing.Enabled = false;
                this.Close();
            }
        }

        private void OnAutoStartComplete(object sender, EventArgs e)
        {
            if (ServiceLoader.AreAllAutoStartServicesRunning())
            {
                //MessageBox.Show("All services started.");
            }
            else
            {
               //MessageBox.Show("Error starting services.", "Servie");
            }
        }

        private void DisplayServiceLoadError(string service, string message)
        {
            MessageBox.Show(message, service, MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            // Trigger the event and clear everything out
            m_SIEvent(m_SISender, m_SIArgs);
            m_SIEvent = null;
            m_SISender = null;
            m_SIArgs = null;
        }
        #endregion
    }
}
