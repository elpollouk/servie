/*
 * A fake server for testing Servie 
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
            if (args.Length >= 2)
            {
                message = args[1];
            }
            else
            {
                message = "Test count {0}";
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
                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}
