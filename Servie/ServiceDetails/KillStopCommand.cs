using System;
using System.Diagnostics;

namespace Servie.ServiceDetails
{
    class KillStopCommand : StopCommandBase, IStopCommand
    {
        public void Stop(Process process, bool blocking)
        {
            process.Kill();
        }
    }
}
