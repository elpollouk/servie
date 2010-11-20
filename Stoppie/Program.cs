/*
 * A simple command line utility to send a CTRL+C event to a specified process.
 * This is needed because it's not possible to do this directly from within a program that use Syste.Diagnotics.Process to launch an application.
 * If you tried to do this within the application, it would also be killed off itself.
 * 
 * Copyright 2010 Adrian O'Grady
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
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

        static int Main(string[] args)
        {
            if ((args != null) && (args.Length > 0))
            {
                int processId = int.Parse(args[0]);
                if (processId != 0)
                {
                    ConsoleCtrlEvent signal;
                    if (args.Length > 1)
                    {
                        // A signal has been specified
                        try
                        {
                            signal = (ConsoleCtrlEvent)Enum.Parse(typeof(ConsoleCtrlEvent), args[1]);
                        }
                        catch
                        {
                            Console.WriteLine("Invalid signal specified.");
                            return -1;
                        }
                    }
                    else
                    {
                        signal = ConsoleCtrlEvent.CTRL_C;
                    }
                    bool r = FreeConsole();
                    if (r == false) return -2;
                    r = AttachConsole(processId);
                    if (r == false) return -3;
                    r = GenerateConsoleCtrlEvent(signal, 0);
                    if (r == false) return -4;
                    return 0;
                }
                else
                {
                    Console.WriteLine("Invalid process id specified.");
                    return -1;
                }
            }
            else
            {
                Console.WriteLine("No process id specified.");
                return -1;
            }
        }
    }
}
