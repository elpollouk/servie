/*
 * Core service implementation.
 * This class controls start and stopping of services along with parsing their servie configuration files.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;

namespace Servie.ServiceDetails
{
    // Thrown if a configuration file indicates that this service should be ignored.
    // This is only an exception because parsing happens in the constructor
    public class IgnoreServiceException : Exception
    {
        public IgnoreServiceException() { }
    }

    // Thrown if there is a problem parsing the configuration file or if required elements are missing.
    public class ParserError : Exception
    {
        public ParserError(string message) : base(message) { }
    }

    public class Service
    {
        private string m_Name = null;
        private string m_DisplayName = null;
        private string m_WorkingDir;
        private Process m_Process;
        private IStopCommand m_StopCommand;
        private bool m_IsRunning = false;
        private int m_ExitCode = 0;
        private bool m_Autostart = false;
        private int m_StartWaitTime = 0;
        private int m_StopTimeOut = 0;
        private bool m_Ignore = false;
        private int m_InvokeId = -1;

        #region Handlers for process events
        // Service TTY events
        public event DataReceivedEventHandler OutputDataReceived
        {
            add
            {
                m_Process.OutputDataReceived += value;
                m_StopCommand.OutputDataReceived += value;
            }
            remove
            {
                m_Process.OutputDataReceived -= value;
                m_StopCommand.OutputDataReceived -= value;
            }
        }
        public event DataReceivedEventHandler ErrorDataReceived
        {
            add
            {
                m_Process.ErrorDataReceived += value;
                m_StopCommand.ErrorDataReceived += value;
            }
            remove
            {
                m_Process.ErrorDataReceived -= value;
                m_StopCommand.ErrorDataReceived -= value;
            }
        }

        // Triggered when a service start request is issued
        private EventHandler _StartRequested;
        public event EventHandler StartRequested
        {
            add
            {
                _StartRequested += value;
            }
            remove
            {
                _StartRequested -= value;
            }
        }

        // Triggered when a service has started
        private EventHandler _Started;
        public event EventHandler Started
        {
            add
            {
                _Started += value;
            }
            remove
            {
                _Started -= value;
            }
        }

        // Triggered when a service stop has been requested
        private EventHandler _StopRequested;
        public event EventHandler StopRequested
        {
            add
            {
                _StopRequested += value;
            }
            remove
            {
                _StopRequested -= value;
            }
        }

        // Triggered when a service has stopped
        private EventHandler _Stopped;
        public event EventHandler Stopped
        {
            add
            {
                _Stopped += value;
            }
            remove
            {
                _Stopped -= value;
            }
        }
        #endregion

        #region Exposed read only properties
        // The exit code returned from the process.
        public int ExitCode
        {
            get { return m_ExitCode; }
        }

        // The name of this service.
        public string Name
        {
            get { return m_Name; }
        }

        // The human readable display name of thos service
        public string DisplayName
        {
            get { return m_DisplayName; }
        }

        // Returns true if this service is currently running.
        public bool IsRunning
        {
            get { return m_IsRunning; }
        }

        // Returns true if this is an auto started service.
        public bool Autostart
        {
            get { return m_Autostart; }
        }

        // The delay needed after starting this service to confirm that it has started correctly.
        public int StartWaitTime
        {
            get { return m_StartWaitTime; }
        }

        // The time needed to stop the service. If the service is still running after this time, then there has been a problem.
        public int StopTimeOut
        {
            get { return m_StopTimeOut; }
        }
        #endregion

        public Service(string name)
        {
            m_WorkingDir = "servers\\" + name; // Generate a default working dir to start with
            if (!File.Exists(m_WorkingDir + "\\servie.xml"))
            {
                // No config so ignore this dir
                throw new IgnoreServiceException();
            }

            // Create process and populate it with the default environment
            m_Process = CreateStandardProccess();
            m_Process.EnableRaisingEvents = true;
            m_Process.Exited += OnEnded;

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

            // Validate that all the required info from the config files has been provided
            ValidateStartInfo();

            m_Name = name;
            if (m_DisplayName == null) m_DisplayName = name;
            m_Process.StartInfo.FileName = Path.GetFullPath(m_Process.StartInfo.FileName);
        }

        private Process CreateStandardProccess()
        {
            Process p = new Process();

            // Configure common process setting for all services
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardInput = true;

            // Add all the vars from the common environment
            foreach (KeyValuePair<string, string> var in ServiceLoader.CommonEnvironment)
            {
               p.StartInfo.EnvironmentVariables[var.Key] = var.Value;
            }

            return p;
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
                m_DisplayName = x.Value;
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

            // Set the working directory again as it might have been changed
            m_WorkingDir = m_Process.StartInfo.WorkingDirectory;

            x = node.Element("wait");
            if (x != null)
            {
                m_StartWaitTime = int.Parse(x.Value);
            }
        }

        // Parse tthe stop command
        private void ParseStop(XElement node)
        {
            foreach (XElement n in node.Descendants())
            {
                if (n.Name == "signal")
                {
                    // Send a signal to the child process
                    m_StopCommand = new StoppieStopCommand(n.Value);
                    break;
                }
                else if (n.Name == "kill")
                {
                    // Plain old kill of the child process
                    m_StopCommand = new KillStopCommand();
                    break;
                }
                else if (n.Name == "exec")
                {
                    // Execute an external command to stop the child process
                    Process stopCommand = null;
                    ExecStopCommand esc = m_StopCommand as ExecStopCommand;

                    // Check if we already have a stop command so that we can update it with this parsed info
                    if (esc == null)
                    {
                        stopCommand = CreateStandardProccess();
                        m_StopCommand = new ExecStopCommand(stopCommand);
                    }
                    else
                    {
                        stopCommand = esc.Command;
                    }

                    ParseExec(stopCommand.StartInfo, n);

                    break;
                }
            }

            XElement x;
            // Time out before prompting to kill the process
            x = node.Element("timeout");
            if (x != null)
            {
                m_StopTimeOut = int.Parse(x.Value);
            }
        }

        // Parse an execution node
        private void ParseExec(ProcessStartInfo startInfo, XElement node)
        {
            XElement x;

            x = node.Element("workingdir");
            if (x != null)
            {
                startInfo.WorkingDirectory = x.Value;
            }
            else
            {
                startInfo.WorkingDirectory = m_WorkingDir;
            }

            x = node.Element("executable");
            if (x != null)
            {
                startInfo.FileName = x.Value;
                if (!File.Exists(startInfo.FileName))
                {
                    startInfo.FileName = Path.Combine(startInfo.WorkingDirectory, startInfo.FileName);
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
                    startInfo.EnvironmentVariables[evar.Name.LocalName] = evar.Value;
                }
            }
        }
        #endregion

        #region Service control functions
        // Start the service if it's not running
        public void Start()
        {
            if (!IsRunning)
            {
                if (_StartRequested != null)
                {
                    _StartRequested(this, null);
                }

                bool r = m_Process.Start();
                if (r == true)
                {
                    m_ExitCode = 0;
                    m_Process.BeginOutputReadLine();
                    m_Process.BeginErrorReadLine();
                    m_IsRunning = true;

                    if (StartWaitTime != 0)
                    {
                        // We need to wait a little bit before we can confirm if the service has started
                        m_InvokeId = Program.ScheduledInvoke(CheckServiceIsRunning, this, null, StartWaitTime);
                    }
                    else
                    {
                        // No need to wait to see if the service has started, just assume it has
                        if (_Started != null)
                        {
                            _Started(this, null);
                        }
                    }
                }
                else
                {
                    // Failed to start the service so clear things up and call event handlers
                    m_ExitCode = -1;
                    m_Process.Close();
                    if (_Stopped != null)
                    {
                        _Stopped(this, null);
                    }
                }
            }
        }

        // Stop a running service
        public void Stop(bool blocking = false)
        {
            if (IsRunning)
            {
                if (_StopRequested != null)
                {
                    _StopRequested(this, null);
                }
                m_Process.StandardInput.Close();
                m_StopCommand.Stop(m_Process, blocking);
            }
        }

        // Kill the service (not a very safe thing to do!)
        public void Kill()
        {
            try
            {
                m_Process.Kill();
            }
            catch { }
        }

        // This function is sheduled to be invoked after StartWaitTime milliseconds.
        // It checks to make sure the service has started and triggers the _Started event.
        private void CheckServiceIsRunning(object sender, EventArgs e)
        {
            // We can probably assume that the service is running
            lock (m_Process)
            {
                if (m_Process.HasExited == false)
                {
                    if (_Started != null)
                    {
                        _Started(this, null);
                    }
                }
            }
        }
        #endregion

        #region Local process event handlers
        // Called by the process when if exits
        private void OnEnded(object sender, EventArgs e)
        {
            // This actually comes from the process thread, so we need to sync with the start wait time incase we fail at the exact time that the check time expires
            lock (m_Process)
            {
                m_IsRunning = false;
                m_ExitCode = m_Process.ExitCode;

                m_Process.CancelOutputRead();
                m_Process.CancelErrorRead();
                m_Process.Close();

                // Cancel our scheduled invoke as we don't need to check anymore
                Program.CancelScheduledInvoke(m_InvokeId);

                if (_Stopped != null)
                {
                    Program.MainWindow.BeginInvoke(_Stopped, this, e);
                }
            }
        }
        #endregion
    }
}
