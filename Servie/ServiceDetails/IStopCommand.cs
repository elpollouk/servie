using System;
using System.Diagnostics;

namespace Servie.ServiceDetails
{
    interface IStopCommand
    {
        void Stop(Process process);
    }
}
