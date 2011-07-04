using System;
using System.Diagnostics;

namespace Servie.ServiceDetails
{
    interface IStopCommand
    {
        void Stop(Process process, bool blocking = false);

        event DataReceivedEventHandler OutputDataReceived;
        event DataReceivedEventHandler ErrorDataReceived;
    }
}
