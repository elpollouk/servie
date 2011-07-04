/*
 * A stop command that invokes an external command to stop the porcess
 */
using System;
using System.Diagnostics;

namespace Servie.ServiceDetails
{
    class ExecStopCommand : StopCommandBase, IStopCommand
    {
        private Process m_Command;

        public ExecStopCommand(Process command)
        {
            m_Command = command;
        }

        public void Stop(Process process, bool blocking)
        {
            m_Command.Start();
            m_Command.WaitForExit();
            m_Command.Close();

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
                m_Command.OutputDataReceived += value;
            }
            remove
            {
                m_Command.OutputDataReceived -= value;
            }
        }
        public event DataReceivedEventHandler ErrorDataReceived
        {
            add
            {
                m_Command.ErrorDataReceived += value;
            }
            remove
            {
                m_Command.ErrorDataReceived -= value;
            }
        }
    }
}