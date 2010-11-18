using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;

namespace Servie
{
    static class Program
    {
        [DllImport("user32.dll")]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndNextChild, string lpClassName, string lpWindowName);
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        public const int WM_USER = 0x400;

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
                IntPtr hwnd = FindWindowEx(IntPtr.Zero, IntPtr.Zero, null, "Servie");
                if (hwnd != IntPtr.Zero)
                {
                    SendMessage(hwnd, WM_USER, 0, 0);
                }
                return;
            }

            ServiceDetails.ServiceLoader.LoadCommonEnvironment();
            ServiceDetails.ServiceLoader.LoadServices(DisplayServiceLoadError);
            if (ServiceDetails.ServiceLoader.NumServices == 0)
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
    }
}
