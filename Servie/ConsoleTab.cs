using System;
using System.Windows.Forms;

namespace Servie
{
    public partial class ConsoleTab : TabPage
    {
        private ServiceDetails.Service m_Service;

        public ServiceDetails.Service Service
        {
            get
            {
                return m_Service;
            }
        }

        public ConsoleTab(ServiceDetails.Service service)
        {
            InitializeComponent();

            m_Service = service;

            m_Service.OutputDataReceived += OnOutputDataReceived;
            m_Service.ErrorDataReceived += OnOutputDataReceived;
            m_Service.Started += OnStarted;
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
                this.Invoke(new Action<string>(AddText), new object[] { text });
                return;
            }

            txtConsole.AppendText(text);
        }

        private void OnOutputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs outLine)
        {
            AddText(outLine.Data + "\n");
        }

        private void OnStarted(object sender, EventArgs e)
        {
            AddText("Starting service...\n");
            cmdStartStop.Text = "Stop";
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
        }

        private void cmdStartStop_Click(object sender, EventArgs e)
        {
            if (m_Service.IsRunning)
            {
                m_Service.Stop();
            }
            else
            {
                m_Service.Start();
            }
        }

        private void cmdClear_Click(object sender, EventArgs e)
        {
            txtConsole.Clear();
        }
    }
}
