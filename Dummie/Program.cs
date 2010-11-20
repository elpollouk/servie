/*
 * A fake server for testing Servie.
 * Usage:
 *     dummie.exe MESSAGE SLEEPTIME NUMMESSAGES
 *
 * As this is just a test program, I'm not going to put too much effort into this!
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

namespace Dummie
{
    class Program
    {
        static void Main(string[] args)
        {
            // Message to display
            string message;
            if (args.Length >= 1)
            {
                message = args[0];
            }
            else
            {
                message = "Test count {0}";
            }

            // Set how long the main loop will sleep for between each message.
            int sleepTime;
            if (args.Length >= 2)
            {
                sleepTime = int.Parse(args[1]);
            }
            else
            {
                sleepTime = 1000;
            }

            // Maximum number of test message to show before exiting
            int maxCount;
            if (args.Length >= 3)
            {
                maxCount = int.Parse(args[2]);
            }
            else
            {
                maxCount = -1;
            }

            int count = 0;
            while (true)
            {
                count++;
                Console.WriteLine(String.Format(message, count));
                if (maxCount >= 0)
                {
                    if (maxCount <= count) return;
                }
                System.Threading.Thread.Sleep(sleepTime);
            }
        }
    }
}
