using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpcGuard
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                
                String appStartupPath = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                //Process process = Process.Start(@"G:\githubrepository\OPCClassicGateWayService\Debug\GateWayServiceUI.exe");
                string exe = appStartupPath + "\\GateWayServiceUI.exe";
                Console.WriteLine("OpcDaemons start " + exe);

                Process process = Process.Start(exe);

                process.WaitForExit();

                Console.WriteLine("OpcDaemons end");

                Thread.Sleep(3000);
            }
        }
    }
}
