/*
 * Main entry point for Servie application.
 * Also contains a simple implementation of a scheduled invoke API.
 * 
 * Copyright 2010 Adrian O'Grady
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Servie
{
    static class Program
    {
        #region WinAPI imports for previous instance communication
        [DllImport("user32.dll")]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndNextChild, string lpClassName, string lpWindowName);
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        public const int WM_USER = 0x400;
        #endregion

        private static frmMain s_MainWindow = null;
        public static frmMain MainWindow
        {
            get { return s_MainWindow; }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Check to see if servie is already running and display it again if it is
            string procName = Process.GetCurrentProcess().ProcessName;
            Process[] prev = Process.GetProcessesByName(procName);
            if (prev.Length > 1)
            {
                // A previous instance is running, so now to find the window for that instance so
                // that we can send it a message to activate
                IntPtr hwnd = FindWindowEx(IntPtr.Zero, IntPtr.Zero, null, "Servie");
                if (hwnd != IntPtr.Zero)
                {
                    SendMessage(hwnd, WM_USER, 0, 0);
                }
                return;
            }

            // Load up all the configuration files
            ServiceDetails.ServiceLoader.LoadCommonEnvironment();
            ServiceDetails.ServiceLoader.LoadServices(DisplayServiceLoadError);
            // Did anything load?
            if (ServiceDetails.ServiceLoader.NumLoadedServices == 0)
            {
                MessageBox.Show("No services loaded.", "Servie", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                s_MainWindow = new frmMain();
                Application.Run(s_MainWindow);
            }
        }

        static void DisplayServiceLoadError(string service, string message)
        {
            MessageBox.Show(message, service, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #region Scheduled invoke API
        // A simple API to schedule an event handler to be called at somepoint in the future
        private static System.Threading.Timer s_Timer = new System.Threading.Timer(OnTimer);
        private static int s_InvokeId = 12345; // Used to make sure a cancel request comes from the code that issued the request
        private static EventHandler m_SIEvent = null;
        private static object m_SISender = null;
        private static EventArgs m_SIArgs = null;

        private static void OnTimer(object o)
        {
            lock (s_Timer)
            {
                // Disable the timer
                s_Timer.Change(Timeout.Infinite, Timeout.Infinite);
                if (m_SIEvent == null) return; // The invoke was cancelled

                EventHandler eventHandler = m_SIEvent;
                object sender = m_SISender;
                EventArgs e = m_SIArgs;
                int oldId = s_InvokeId;

                // Clear everything out so a new event can be raised
                m_SIEvent = null;
                m_SISender = null;
                m_SIArgs = null;

                // Trigger the event using the cached values
                MainWindow.Invoke(eventHandler, sender, e);
                if (oldId == s_InvokeId)
                {
                    // Invalidate the current invoke id if a new invoke hasn't been scheduled
                    // This is just a precaution
                    s_InvokeId++;
                }
            }
        }

        // Schedule an event handler to be invoked in the future
        public static int ScheduledInvoke(EventHandler evnt, object sender, EventArgs args, int delay)
        {
            if (m_SIEvent != null) throw new Exception("An event is already scheduled.");
            if (evnt == null) throw new ArgumentNullException();

            m_SIEvent = evnt;
            m_SISender = sender;
            m_SIArgs = args;

            s_Timer.Change(delay, Timeout.Infinite);
            s_InvokeId++;
            return s_InvokeId;
        }

        // Cancel a scheduled invoke
        public static void CancelScheduledInvoke(int invokeId)
        {
            lock (s_Timer)
            {
                if (invokeId == s_InvokeId)
                {
                    s_Timer.Change(Timeout.Infinite, Timeout.Infinite);
                    m_SIEvent = null;
                    m_SISender = null;
                    m_SIArgs = null;
                    s_InvokeId++;
                }
            }
        }
        #endregion
    }
}
