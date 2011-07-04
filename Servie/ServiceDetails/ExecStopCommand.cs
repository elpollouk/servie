/*
 * A stop command that invokes an external command to stop the porcess
 */
using System;
using System.Diagnostics;

namespace Servie.ServiceDetails
{
    class ExecStopCommand : StopCommandBase, IStopCommand
    {
        public ExecStopCommand(Process command)
        {
            Command = command;
        }

        public Process Command
        {
            get;
            private set;
        }

        public void Stop(Process process, bool blocking)
        {
            Command.Start();
            Command.BeginOutputReadLine();
            Command.BeginErrorReadLine();
            Command.WaitForExit();
            Command.CancelOutputRead();
            Command.CancelErrorRead();
            Command.Close();

            if (blocking)
            {
                BlockingStop(process);
            }
        }


        // Service TTY events
        public event DataReceivedEventHandler OutputDataReceived
        {
            add
            {
                Command.OutputDataReceived += value;
            }
            remove
            {
                Command.OutputDataReceived -= value;
            }
        }
        public event DataReceivedEventHandler ErrorDataReceived
        {
            add
            {
                Command.ErrorDataReceived += value;
            }
            remove
            {
                Command.ErrorDataReceived -= value;
            }
        }
    }
}