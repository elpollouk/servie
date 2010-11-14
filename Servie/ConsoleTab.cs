using System;
using System.Windows.Forms;

namespace Servie
{
    public partial class ConsoleTab : TabPage
    {
        private ServiceDetails.Service m_Service;

        public ServiceDetails.Service Service
        {
            get { return m_Service; }
        }

        public ConsoleTab(ServiceDetails.Service service)
        {
            InitializeComponent();

            m_Service = service;

            m_Service.OutputDataReceived += OnOutputDataReceived;
            m_Service.ErrorDataReceived += OnOutputDataReceived;
            m_Service.Started += OnStartBegin;
            m_Service.Ended += OnEnded;

            Text = service.Name;
            cmdStartStop.Text = "Start";
            DoubleBuffered = true;
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
            AddText(outLine.Data + "\n");
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
        }

        private void cmdStartStop_Click(object sender, EventArgs e)
        {
            cmdStartStop.Enabled = false;
            if (m_Service.IsRunning)
            {
                cmdStartStop.Text = "Start";
                m_Service.Stop();
            }
            else
            {
                ServiceDetails.ServiceLoader.StartService(m_Service, OnStartComplete, null);
            }
        }

        private void cmdClear_Click(object sender, EventArgs e)
        {
            txtConsole.Clear();
        }
    }
}
