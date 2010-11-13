/*
 * A simple command line utility to send a CTRL+C event to a specified process.
 * This is needed because it's not possible to do this directly from within a program that use Syste.Diagnotics.Process to launch an application.
 * If you tried to do this within the application, it would also be killed off itself.
 */
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Stoppie
{
    class Program
    {
        [DllImport("kernel32.dll")]
        static extern bool GenerateConsoleCtrlEvent(ConsoleCtrlEvent sigevent, int dwProcessGroupId);
        [DllImport("kernel32.dll")]
        static extern bool FreeConsole();
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);

        public enum ConsoleCtrlEvent
        {
            CTRL_C = 0, // From wincom.h
            CTRL_BREAK = 1,
            CTRL_CLOSE = 2,
            CTRL_LOGOFF = 5,
            CTRL_SHUTDOWN = 6
        }

        static void Main(string[] args)
        {
            if ((args != null) && (args.Length > 0))
            {
                int processId = int.Parse(args[0]);
                if (processId != 0)
                {
                    FreeConsole();
                    AttachConsole(processId);
                    GenerateConsoleCtrlEvent(ConsoleCtrlEvent.CTRL_C, 0);
                }
                else
                {
                    Console.WriteLine("Invalid process id specified.");
                }
            }
            else
            {
                Console.WriteLine("No process id specified.");
            }
        }
    }
}
