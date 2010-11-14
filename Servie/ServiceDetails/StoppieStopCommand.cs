/*
 * Stop command that works by invoking the Stoppie utility
 */
using System;
using System.Diagnostics;

namespace Servie.ServiceDetails
{
    class StoppieStopCommand : IStopCommand
    {
        private static readonly string kStoppiePath = "bin\\stoppie.exe";
        private string m_Signal;

        public StoppieStopCommand(string signal)
        {
            m_Signal = signal;
        }

        public void Stop(Process process)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = kStoppiePath;
            p.StartInfo.Arguments = process.Id.ToString() + " " + m_Signal;

            p.Start();
            p.WaitForExit();
            p.Close();
        }
    }
}
