using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace Servie
{
    public partial class ConsoleTab : TabPage
    {
        private Process m_Process;
        private string m_Command;
        private bool m_IsRunning = false;

        public ConsoleTab(string command, string args = null, Dictionary<string, string> environment = null)
        {
            InitializeComponent();

            m_Command = command;

            m_Process = new Process();
            m_Process.StartInfo.UseShellExecute = false;
            m_Process.StartInfo.CreateNoWindow = true;
            m_Process.StartInfo.RedirectStandardOutput = true;
            m_Process.StartInfo.RedirectStandardError = true;

            if (args != null)
            {
                m_Process.StartInfo.Arguments = args;
            }

            if (environment != null)
            {
                foreach (KeyValuePair<string, string> var in environment)
                {
                    m_Process.StartInfo.EnvironmentVariables.Add(var.Key, var.Value);
                }
            }

            m_Process.EnableRaisingEvents = true;

            m_Process.OutputDataReceived += OnOutputDataReceived;
            m_Process.ErrorDataReceived += OnOutputDataReceived;
            m_Process.Exited += OnEnded;

            m_Process.StartInfo.FileName = m_Command;

            cmdStartStop.Text = "Start";
        }

        public bool Start()
        {
            AddText("Starting " + m_Command + "...\n");
            m_IsRunning = true;
            bool r = m_Process.Start();
            if (r == false)
            {
                AddText("Failed.");
            }
            else
            {
                m_Process.BeginOutputReadLine();
                m_Process.BeginErrorReadLine();
                AddText("Process id = " + m_Process.Id + "\n");

                cmdStartStop.Text = "Stop";
            }

            return r;
        }

        public void Stop()
        {
            if (IsRunning)
            {
                Process p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.FileName = Constants.kStoppiePath;
                p.StartInfo.Arguments = m_Process.Id.ToString();

                p.Start();
                p.WaitForExit();
                p.Close();

                cmdStartStop.Text = "Start";
            }
        }

        public bool IsRunning
        {
            get { return m_IsRunning; }
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

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs outLine)
        {
            AddText(outLine.Data + "\n");
        }

        private void OnEnded(object sender, EventArgs e)
        {
//            string data = m_Process.StandardOutput.ReadToEnd();
//            AddText(data);
            m_IsRunning = false;
            AddText("\nService exited with " + m_Process.ExitCode + "\n");

            m_Process.CancelOutputRead();
            m_Process.CancelErrorRead();
            m_Process.Close();
        }


        private void cmdStartStop_Click(object sender, EventArgs e)
        {
            if (m_IsRunning)
            {
                Stop();
            }
            else
            {
                Start();
            }
        }

        private void cmdClear_Click(object sender, EventArgs e)
        {
            txtConsole.Clear();
        }
    }
}
