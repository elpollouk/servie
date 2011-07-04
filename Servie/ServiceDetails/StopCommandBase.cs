using System;
using System.Diagnostics;

namespace Servie.ServiceDetails
{
    class StopCommandBase
    {
        protected void BlockingStop(Process process)
        {
            process.EnableRaisingEvents = false;
            process.CancelErrorRead();
            process.CancelOutputRead();
            process.WaitForExit();
        }

        public event DataReceivedEventHandler OutputDataReceived
        {
            add {}
            remove {}
        }
        public event DataReceivedEventHandler ErrorDataReceived
        {
            add {}
            remove {}
        }
    }
}
