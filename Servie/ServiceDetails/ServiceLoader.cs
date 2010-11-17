using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace Servie.ServiceDetails
{
    class ServiceLoader
    {
        private const string kCommonEnvironmentPath = "packages\\servie\\environment.xml";
        private const string kLocalEnvironmentPath = "packages\\servie\\localconf\\environment.xml";

        public delegate void ErrorMessageHandler(string serviceName, string message);

        private static ImmutableDictionary<string, Service> s_ImmutableServices = null;
        private static Dictionary<string, Service> s_Services = null;
        private static ImmutableDictionary<string, string> s_ImmutableEnv = null;
        private static Dictionary<string, string> s_Environment = null;

        private static EventHandler _OnAutoStartComplete = null;
        private static ErrorMessageHandler _DisplayError = null;
        private static Stack<Service> s_AutoStartStack = null;

        // Returns a plain list of services 
        public static IEnumerable<Service> Services
        {
            get { return s_Services.Values; }
        }

        // Returns an immutable dictionary of services
        public static IDictionary<string, Service> ServiceDictionary
        {
            get { return s_ImmutableServices; }
        }

        // Returns a dictionary of common environmental variables
        public static IDictionary<string, string> CommonEnvironment
        {
            get { return s_ImmutableEnv; }
        }

        // Loads the common environment variables
        public static void LoadCommonEnvironment()
        {
            if (s_Environment == null)
            {
                s_Environment = new Dictionary<string, string>();
                s_ImmutableEnv = new ImmutableDictionary<string, string>(s_Environment);
            }

            s_Environment.Clear();

            if (File.Exists(kCommonEnvironmentPath))
            {
                XDocument doc = XDocument.Load(kCommonEnvironmentPath);
                if (doc != null) ParseEnvironment(doc.Root);
            }

            if (File.Exists(kLocalEnvironmentPath))
            {
                XDocument doc = XDocument.Load(kLocalEnvironmentPath);
                if (doc != null) ParseEnvironment(doc.Root);
            }
        }

        private static void ParseEnvironment(XElement node)
        {
            foreach (XElement evar in node.Descendants())
            {
                if (s_Environment.ContainsKey(evar.Name.LocalName))
                {
                    s_Environment.Remove(evar.Name.LocalName);
                }
                s_Environment.Add(evar.Name.LocalName, evar.Value);
            }
        }

        // Loads all the services found in the servers directory
        public static void LoadServices(ErrorMessageHandler onError)
        {
            s_Services = new Dictionary<string, Service>();
            s_ImmutableServices = new ImmutableDictionary<string, Service>(s_Services);

            // Just to ensure an empty environment is available at least
            if (s_Environment == null)
            {
                s_Environment = new Dictionary<string, string>();
                s_ImmutableEnv = new ImmutableDictionary<string, string>(s_Environment);
            }

            // Get a list of all the services in the environment and try to load them
            try
            {
                foreach (string dir in Directory.EnumerateDirectories("servers"))
                {
                    string serviceName = Path.GetFileName(dir);
                    try
                    {
                        // Skip directories starting with "."
                        if (serviceName.StartsWith(".") == false)
                        {
                            ServiceDetails.Service service = new ServiceDetails.Service(serviceName);
                            s_Services.Add(serviceName, service);
                        }
                    }
                    catch (ServiceDetails.IgnoreServiceException)
                    {
                        // This service has been flagged as to be ignored
                    }
                    catch (ServiceDetails.ParserError x)
                    {
                        if (onError != null)
                        {
                            onError(serviceName, x.Message);
                        }
                    }
                }
            }
            catch (DirectoryNotFoundException)
            {
                System.Windows.Forms.MessageBox.Show("Server directory not found.", "Servie", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        #region Bulk service control
        // Autostarts services
        public static void AutoStartServices(EventHandler onAutoStartComplete, ErrorMessageHandler displayError)
        {
            if (s_AutoStartStack != null) throw new Exception("Services are already being started.");
            _OnAutoStartComplete = onAutoStartComplete;
            _DisplayError = displayError;

            // Very slack approach to making sure the services are on the stack in the right order, but at least I know
            // it will work.
            s_AutoStartStack = new Stack<Service>();
            Stack<Service> reverseStack = new Stack<Service>();

            // Push all the non-running autostart services onto the stack
            foreach (Service service in s_Services.Values)
            {
                if (service.Autostart && !service.IsRunning)
                {
                    reverseStack.Push(service);
                }
            }

            // Now to actually do the reversing
            while (reverseStack.Count != 0)
            {
                Service item = reverseStack.Pop();
                s_AutoStartStack.Push(item);
            }

            StartFirstServiceOnStack();
        }

        // Start a single service
        public static void StartService(Service service, EventHandler onStartComplete, ErrorMessageHandler displayError)
        {
            if (s_AutoStartStack != null) throw new Exception("Services are already being started.");
            _OnAutoStartComplete = onStartComplete;
            _DisplayError = displayError;

            s_AutoStartStack = new Stack<Service>();

            // Push the service onto the stack
            if (!service.IsRunning)
            {
                s_AutoStartStack.Push(service);
            }

            StartFirstServiceOnStack();
        }

        // Stop all running services
        public static void StopAllServices(bool blocking = false)
        {
            foreach (Service service in s_Services.Values)
            {
                if (service.IsRunning) service.Stop(blocking);
            }
        }

        public static bool AreAllAutoStartServicesRunning()
        {
            foreach (Service service in s_Services.Values)
            {
                if (service.Autostart && !service.IsRunning) return false;
            }
            return true;
        }

        public static bool AreAllServicesRunning()
        {
            foreach (Service service in s_Services.Values)
            {
                if (!service.IsRunning) return false;
            }
            return true;
        }

        public static bool AreAllServicesStopped()
        {
            foreach (Service service in s_Services.Values)
            {
                if (service.IsRunning) return false;
            }
            return true;
        }

        private static void StartNextService(object sender, EventArgs e)
        {
            // Pop the current service off the top of the stack and check if it managed to start
            Service service = s_AutoStartStack.Pop();
            if (!service.IsRunning)
            {
                if (_DisplayError != null) _DisplayError(service.Name, "Service failed to start.");
            }
            StartFirstServiceOnStack();
        }

        //Starts the first service on the stack
        private static void StartFirstServiceOnStack()
        {
            Service firstService;
            // Make sure there are services on the stack
            while (s_AutoStartStack.Count > 0)
            {
                firstService = s_AutoStartStack.Peek();
                firstService.Start();
                if (firstService.StartWaitTime != 0)
                {
                    // Schedule the start next service function to be called after the wait period
                    Program.MainWindow.ScheduledInvoke(StartNextService, s_AutoStartStack, null, firstService.StartWaitTime);
                    break;
                }
                else
                {
                    // No delay after calling start, so pop it from the stack
                    s_AutoStartStack.Pop();
                }
            }

            // If there are no more services on the start, report the starting as complete
            if (s_AutoStartStack.Count == 0)
            {
                OnAutoStartComplete();
            }
        }

        private static void OnAutoStartComplete()
        {
            if (_OnAutoStartComplete != null) _OnAutoStartComplete(null, null);

            // Clear everything out
            _OnAutoStartComplete = null;
            _DisplayError = null;
            s_AutoStartStack = null;
        }
        #endregion
    }
}
