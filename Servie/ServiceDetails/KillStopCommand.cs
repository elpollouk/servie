using System;
using System.Diagnostics;

namespace Servie.ServiceDetails
{
    class KillStopCommand : IStopCommand
    {
        public void Stop(Process process)
        {
            process.Kill();
        }
    }
}
