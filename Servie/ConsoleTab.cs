/*
 * Custom TabPage control that contains all the controls for the output console.
 * If you need to edit this in the designer, then easiest way to do that is to make UserControl the
 * base and change it before you compile again.
 */
using System;
using System.Windows.Forms;
using Servie.ServiceDetails;

namespace Servie
{
    public partial class ConsoleTab : TabPage
    {
        private Service m_Service;

        public Service Service
        {
            get { return m_Service; }
        }

        public ConsoleTab(Service service)
        {
            InitializeComponent();

            m_Service = service;

            // Register all out event handlers
            m_Service.OutputDataReceived += OnOutputDataReceived;
            m_Service.ErrorDataReceived += OnOutputDataReceived;
            m_Service.StartRequested += OnStartRequested;
            m_Service.Started += OnStarted;
            m_Service.StopRequested += OnStopRequested;
            m_Service.Stopped += OnStopped;

            Text = service.DisplayName;
            UseVisualStyleBackColor = true;

            cmdStartStop.Text = "Start";
        }

        private void cmdStartStop_Click(object sender, EventArgs e)
        {
            if (m_Service.IsRunning)
            {
                // The service is running, so issue a stop request
                m_Service.Stop();
            }
            else
            {
                // The service isn't running, so use the ServiceLoader to start it rather than
                // calling Start() directly. This is so that an dependencies can be resolved by the
                // ServiceLoader and also started if needed.
                if (ServiceLoader.IsStartingService == false)
                {
                    ServiceLoader.StartService(m_Service, null);
                }
            }
        }

        private void cmdClear_Click(object sender, EventArgs e)
        {
            txtConsole.Clear();
        }

        private void timerStopping_Tick(object sender, EventArgs e)
        {
            timerStopping.Enabled = false;
            DialogResult result = MessageBox.Show("Service has failed to stop, do you want to continue waiting?", m_Service.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                timerStopping.Enabled = true;
            }
            else
            {
                m_Service.Kill();
            }
        }

        #region Service event handlers
        private void OnStartRequested(object sender, EventArgs e)
        {
            ((TabControl)Parent).SelectedTab = this; // Focus on this tab
            cmdStartStop.Enabled = false;

            AddText("Starting service...\r\n");
        }

        private void OnStarted(object sender, EventArgs e)
        {
            cmdStartStop.Enabled = true;
            cmdStartStop.Text = "Stop";
        }

        private void OnStopRequested(object sender, EventArgs e)
        {
            cmdStartStop.Enabled = false;
            if (m_Service.StopTimeOut != 0)
            {
                timerStopping.Interval = m_Service.StopTimeOut;
                timerStopping.Enabled = true;
            }

            AddText("Stopping service...\r\n");
        }

        private void OnStopped(object sender, EventArgs e)
        {
            // This can actually be called from the process thread.
            if (InvokeRequired)
            {
                BeginInvoke(new EventHandler(OnStopped), sender, e);
                return;
            }

            AddText("\r\nService exited with " + m_Service.ExitCode + "\r\n");
            cmdStartStop.Text = "Start";
            cmdStartStop.Enabled = true;

            timerStopping.Enabled = false;
        }

        private void OnOutputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs outLine)
        {
            AddText(outLine.Data + "\r\n");
        }
        #endregion

        private void AddText(string text)
        {
            if (text == null) return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AddText), text);
                return;
            }

            txtConsole.AppendText(text);
        }
    }
}
