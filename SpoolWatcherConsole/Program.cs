using System;
using System.Collections.Generic;
using System.Text;
using Domain;
using log4net.Config;

namespace SpoolWatcherConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            Console.WriteLine("Type 'exit' and hit enter at anytime to stop the application");

            Monitor monitor = new Monitor();
            monitor.Run();

            string exit = Console.ReadLine();
            while(exit.ToLower() != "exit")
            {
                exit = Console.ReadLine();
            }
            
            monitor.Kill();
            
        }
    }
}
