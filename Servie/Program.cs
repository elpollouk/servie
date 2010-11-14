using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Servie
{
    static class Program
    {
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
            ServiceDetails.ServiceLoader.LoadCommonEnvironment();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            s_MainWindow = new frmMain();
            Application.Run(s_MainWindow);
        }
    }
}
