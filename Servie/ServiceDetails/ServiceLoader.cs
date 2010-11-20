/*
 * Controls loading of all the services in the environment.
 * Also controls starting services as it can resolve dependancies (when implemented).
 */
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

        private static ErrorMessageHandler _DisplayError = null;
        private static Stack<Service> s_AutoStartStack = null;

        #region Properties
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

        // Return the number of loaded services
        public static int NumLoadedServices
        {
            get { return s_Services.Count; }
        }

        // Return the number of running services
        public static int NumRunningServices
        {
            get
            {
                int count = 0;
                foreach (Service service in s_Services.Values)
                {
                    if (service.IsRunning)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        // Returns true if a service is currently starting
        public static bool IsStartingService
        {
            get { return s_AutoStartStack != null; }
        }

        // Returns true is all autostart services are running
        public static bool AreAllAutoStartServicesRunning
        {
            get
            {
                foreach (Service service in s_Services.Values)
                {
                    if (service.Autostart && !service.IsRunning) return false;
                }
                return true;
            }
        }

        // Returns true if all services are running
        public static bool AreAllServicesRunning
        {
            get
            {
                foreach (Service service in s_Services.Values)
                {
                    if (!service.IsRunning) return false;
                }
                return true;
            }
        }

        // Returns true if no services are running
        public static bool AreAllServicesStopped
        {
            get
            {
                foreach (Service service in s_Services.Values)
                {
                    if (service.IsRunning) return false;
                }
                return true;
            }
        }
        #endregion

        #region Bulk service start/stop events
        // Triggered when a bulk service start request is issued
        private static EventHandler _StartRequested;
        public static event EventHandler StartRequested
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

        // Triggered when a bulk service start request has completed
        private static EventHandler _Started;
        public static event EventHandler Started
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
        #endregion

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

            try
            {
                // Get a list of all the services in the environment and try to load them
                foreach (string dir in Directory.EnumerateDirectories("servers"))
                {
                    string serviceName = Path.GetFileName(dir);
                    try
                    {
                        // Skip directories starting with "." or "!" (because windows explorer won't allow you to create a directory starting with '.')
                        if ((serviceName.StartsWith(".") == false) && (serviceName.StartsWith("!") == false))
                        {
                            ServiceDetails.Service service = new ServiceDetails.Service(serviceName);
                            s_Services.Add(service.Name, service);
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
                onError("Servie", "Servers directory not found.");
            }
        }

        #region Bulk service control
        // Autostarts services
        public static void AutoStartServices(ErrorMessageHandler displayError)
        {
            if (s_AutoStartStack != null) throw new Exception("Services are already being started.");
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

            // Now to reverse the order of the stack
            while (reverseStack.Count != 0)
            {
                Service item = reverseStack.Pop();
                s_AutoStartStack.Push(item);
            }

            if (_StartRequested != null) _StartRequested(null, null);
            StartFirstServiceOnStack();
        }

        // Start a single service
        public static void StartService(Service service, ErrorMessageHandler displayError)
        {
            if (s_AutoStartStack != null) throw new Exception("Services are already being started.");
            _DisplayError = displayError;

            s_AutoStartStack = new Stack<Service>();

            // Push the service onto the stack
            if (!service.IsRunning)
            {
                s_AutoStartStack.Push(service);
            }

            if (_StartRequested != null) _StartRequested(null, null);
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

        //Starts the first service on the stack
        private static void StartFirstServiceOnStack()
        {
            // If there are no more services on the start, report the starting as complete
            if (s_AutoStartStack.Count == 0)
            {
                OnAutoStartComplete();
            }
            else
            {
                Service firstService = s_AutoStartStack.Peek();
                firstService.Started += OnServiceStarted;
                firstService.Stopped += OnServiceStopped;
                firstService.Start();
            }
        }

        private static void OnServiceStarted(object sender, EventArgs e)
        {
            // Service started successfully
            Service service = s_AutoStartStack.Pop();
            if (service != sender) throw new Exception("Callback from unknown service.");
            // Unregister our event handlers
            service.Started -= OnServiceStarted;
            service.Stopped -= OnServiceStopped;

            StartFirstServiceOnStack();
        }

        private static void OnServiceStopped(object sender, EventArgs e)
        {
            // Service failed to start
            Service service = s_AutoStartStack.Pop();
            if (service != sender) throw new Exception("Callback from unknown service.");
            if (_DisplayError != null) _DisplayError(service.Name, "Service failed to start.");
            // Unregister our event handlers
            service.Started -= OnServiceStarted;
            service.Stopped -= OnServiceStopped;

            StartFirstServiceOnStack();
        }

        private static void OnAutoStartComplete()
        {
            if (_Started != null) _Started(null, null);

            // Clear everything out
            _DisplayError = null;
            s_AutoStartStack = null;
        }
        #endregion
    }
}
