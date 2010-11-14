﻿using System;
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
        public delegate void ScheduledInvokeHandler(EventHandler evnt, object sender, EventArgs args, int delay);

        private static ImmutableDictionary<string, Service> s_ImmutableServices = null;
        private static Dictionary<string, Service> s_Services = null;
        private static ImmutableDictionary<string, string> s_ImmutableEnv = null;
        private static Dictionary<string, string> s_Environment = null;

        private static ScheduledInvokeHandler _ScheduledInvoke = null;
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

        #region Bulk service control
        // Autostarts services
        public static void AutoStartServices(ScheduledInvokeHandler scheduledInvoke, EventHandler onAutoStartComplete, ErrorMessageHandler displayError)
        {
            _ScheduledInvoke = scheduledInvoke;
            _OnAutoStartComplete = onAutoStartComplete;
            _DisplayError = displayError;

            s_AutoStartStack = new Stack<Service>();

            // Push all the non-running autostart services onto the stack
            foreach (Service service in s_Services.Values)
            {
                if (service.Autostart && !service.IsRunning)
                {
                    s_AutoStartStack.Push(service);
                }
            }

            StartFirstServiceOnStack();
        }

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
            Service service = s_AutoStartStack.Pop();
            if (!service.IsRunning)
            {
                if (_DisplayError != null) _DisplayError(service.Name, "Service failed to start.");
            }
            StartFirstServiceOnStack();
        }

        private static void StartFirstServiceOnStack()
        {
            Service firstService;
            while (s_AutoStartStack.Count > 0)
            {
                firstService = s_AutoStartStack.Peek();
                firstService.Start();
                if (firstService.StartWaitTime != 0)
                {
                    _ScheduledInvoke(StartNextService, s_AutoStartStack, null, firstService.StartWaitTime);
                    break;
                }
                else
                {
                    s_AutoStartStack.Pop();
                }
            }

            if (s_AutoStartStack.Count == 0)
            {
                OnAutoStartComplete();
            }
        }

        private static void OnAutoStartComplete()
        {
            if (_OnAutoStartComplete != null) _OnAutoStartComplete(null, null);

            _OnAutoStartComplete = null;
            _ScheduledInvoke = null;
            _DisplayError = null;
        }
        #endregion
    }
}
