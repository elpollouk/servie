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

            m_Service.OutputDataReceived += OnOutputDataReceived;
            m_Service.ErrorDataReceived += OnOutputDataReceived;
            m_Service.Started += OnStartBegin;
            m_Service.Ended += OnEnded;

            Text = service.Name;
            UseVisualStyleBackColor = true;

            cmdStartStop.Text = "Start";
        }

        private void AddText(string text)
        {
            if (text == null) return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AddText), text );
                return;
            }

            txtConsole.AppendText(text);
        }

        private void OnOutputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs outLine)
        {
            AddText(outLine.Data + "\r\n");
        }

        private void OnStartBegin(object sender, EventArgs e)
        {
            cmdStartStop.Text = "Stop";
            AddText("Starting service...\n");
        }

        private void OnStartComplete(object sender, EventArgs e)
        {
            cmdStartStop.Enabled = true;
            if (!m_Service.IsRunning)
            {
                cmdStartStop.Text = "Start";
            }
        }

        private void OnEnded(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new EventHandler(OnEnded), sender, e);
                return;
            }

            AddText("\nService exited with " + m_Service.ExitCode + "\n");
            cmdStartStop.Text = "Start";
            cmdStartStop.Enabled = true;
            timerStopping.Enabled = false;
        }

        private void cmdStartStop_Click(object sender, EventArgs e)
        {
            cmdStartStop.Enabled = false;
            if (m_Service.IsRunning)
            {
                if (m_Service.StopTimeOut != 0)
                {
                    timerStopping.Interval = m_Service.StopTimeOut;
                    timerStopping.Enabled = true;
                }
                cmdStartStop.Text = "Start";
                m_Service.Stop();
            }
            else
            {
                if (ServiceLoader.IsStartingService == false)
                {
                    ServiceLoader.StartService(m_Service, OnStartComplete, null);
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
            DialogResult result =  MessageBox.Show("Service has failed to stop, do you want to continue waiting?", m_Service.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                timerStopping.Enabled = true;
            }
            else
            {
                m_Service.Kill();
            }
        }
    }
}
