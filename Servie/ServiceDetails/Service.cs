using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;

namespace Servie.ServiceDetails
{
    public class IgnoreServiceException : Exception
    {
        public IgnoreServiceException() { }
    }

    public class ParserError : Exception
    {
        public ParserError(string message) : base(message) { }
    }

    public class Service
    {
        private string m_Name = null;
        private string m_WorkingDir;
        private Process m_Process;
        private IStopCommand m_StopCommand;
        private bool m_IsRunning = false;
        private int m_ExitCode = 0;
        private bool m_Autostart = false;
        private bool m_Ignore = false;

        #region Handlers for process events
        public event DataReceivedEventHandler OutputDataReceived
        {
            add
            {
                m_Process.OutputDataReceived += value;
            }
            remove
            {
                m_Process.OutputDataReceived -= value;
            }
        }

        public event DataReceivedEventHandler ErrorDataReceived
        {
            add
            {
                m_Process.ErrorDataReceived += value;
            }
            remove
            {
                m_Process.ErrorDataReceived -= value;
            }
        }

        private EventHandler _Ended;
        public event EventHandler Ended
        {
            add
            {
                _Ended += value;
            }
            remove
            {
                _Ended -= value;
            }
        }

        private EventHandler _Started;
        public event EventHandler Started
        {
            add
            {
                _Started += value;
            }
            remove
            {
                _Started += value;
            }
        }
        #endregion

        #region Exposed read only properties
        public int ExitCode
        {
            get { return m_ExitCode; }
        }

        public string Name
        {
            get { return m_Name; }
        }

        public bool IsRunning
        {
            get { return m_IsRunning; }
        }

        public bool Autostart
        {
            get { return m_Autostart; }
        }
        #endregion

        public Service(string name)
        {
            m_Process = new Process();
            m_WorkingDir = "servers\\" + name; // Generate a default working dir to start with

            if (!File.Exists(m_WorkingDir + "\\servie.xml"))
            {
                // No config so ignore this dir
                throw new IgnoreServiceException();
            }

            // Parse the server supplied details
            XDocument doc = XDocument.Load(m_WorkingDir + "\\servie.xml");
            Parse(doc.Root);

            // Now pick up any local overrides.
            string localConf = "packages\\servie\\localconf\\" + name + ".xml";
            if (File.Exists(localConf))
            {
                doc = XDocument.Load(localConf);
                Parse(doc.Root);
            }

            ValidateStartInfo();

            if (m_Name == null) m_Name = name;
            m_Process.StartInfo.FileName = Path.GetFullPath(m_Process.StartInfo.FileName);

            // Configure common process setting for all services
            m_Process.StartInfo.UseShellExecute = false;
            m_Process.StartInfo.CreateNoWindow = true;
            m_Process.StartInfo.RedirectStandardOutput = true;
            m_Process.StartInfo.RedirectStandardError = true;

            m_Process.EnableRaisingEvents = true;
            m_Process.Exited += OnEnded;
        }

        // Validate that the service has been configured correctly
        private void ValidateStartInfo()
        {
            if (m_Ignore) throw new IgnoreServiceException();

            if (m_Process.StartInfo.FileName == null) throw new ParserError("No executable specified.");
            if (!File.Exists(m_Process.StartInfo.FileName)) throw new ParserError("Executable " + m_Process.StartInfo.FileName + " was not found.");
            if (!Directory.Exists(m_Process.StartInfo.WorkingDirectory)) throw new ParserError("Directory " + m_Process.StartInfo.FileName + " was not found.");
            if (m_StopCommand == null) throw new ParserError("No stop command specified.");
        }

        #region XML parsing
        // Parse a service config XML
        private void Parse(XElement root)
        {
            XElement x;

            // Check that we don't need to ignore this service.
            x = root.Element("ignore");
            if (x != null)
            {
                m_Ignore = (x.Value.ToLower() == "true");
            }

            // Set up the basic service details
            x = root.Element("name");
            if (x != null)
            {
                m_Name = x.Value;
            }

            // Is this an autostarted service?
            x = root.Element("autostart");
            if (x != null)
            {
                m_Autostart = (x.Value.ToLower() == "true");
            }

            x = root.Element("start");
            if (x != null) ParseStart(x);

            x = root.Element("stop");
            if (x != null) ParseStop(x);
        }

        // Parse the start command
        private void ParseStart(XElement node)
        {
            XElement x = node.Element("exec");
            if (x != null) ParseExec(m_Process.StartInfo, x);
        }

        // Parse tthe stop command
        private void ParseStop(XElement node)
        {
            foreach (XElement x in node.Descendants())
            {
                if (x.Name == "signal")
                {
                    m_StopCommand = new StoppieStopCommand(x.Value);
                    return;
                }
                else if (x.Name == "kill")
                {
                    m_StopCommand = new KillStopCommand();
                    return;
                }
            }
        }

        // Parse an execution node
        private void ParseExec(ProcessStartInfo startInfo, XElement node)
        {
            XElement x;
            x = node.Element("workingdir");
            if (x != null)
            {
                m_WorkingDir = x.Value;
            }
            startInfo.WorkingDirectory = m_WorkingDir;

            x = node.Element("executable");
            if (x != null)
            {
                startInfo.FileName = x.Value;
                if (!File.Exists(startInfo.FileName))
                {
                    startInfo.FileName = Path.Combine(m_WorkingDir, startInfo.FileName);
                }
            }

            x = node.Element("args");
            if (x != null)
            {
                startInfo.Arguments = x.Value;
            }

            x = node.Element("env");
            if (x != null)
            {
                foreach (XElement evar in x.Descendants())
                {
                    if (startInfo.EnvironmentVariables.ContainsKey(evar.Name.LocalName))
                    {
                        startInfo.EnvironmentVariables.Remove(evar.Name.LocalName);
                    }
                    startInfo.EnvironmentVariables.Add(evar.Name.LocalName, evar.Value);
                }
            }
        }
        #endregion

        #region Service control functions
        public void Start()
        {
            bool r = m_Process.Start();
            if (r)
            {
                m_ExitCode = 0;
                m_Process.BeginOutputReadLine();
                m_Process.BeginErrorReadLine();
                m_IsRunning = true;
                _Started(this, null);
            }
        }

        public void Stop()
        {
            if (IsRunning)
            {
                m_StopCommand.Stop(m_Process);
            }
        }
        #endregion

        #region Local process event handlers
        private void OnEnded(object sender, EventArgs e)
        {
            m_IsRunning = false;
            m_ExitCode = m_Process.ExitCode;

            _Ended(this, e);

            m_Process.CancelOutputRead();
            m_Process.CancelErrorRead();
            m_Process.Close();
        }
        #endregion
    }
}
