using DLIOutletController;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Net;
using System.Threading.Tasks;

namespace OutletController
{
    class Program
    {
        enum Commands
        {
            Unknown,
            GetStatus,
            Cycle,
            SetSwitch
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Brendan's DLI Outlet Controller - v1.0");
            if( args == null || args.Length == 0)
            {
                ShowUsage();
                return;
            }

            Commands theCommand = Commands.Unknown;
            int index = -1;
            bool desiredState = true;
            var ci = new ConnectionInfo();

            IPAddress ip;
            if (!IPAddress.TryParse(args[0], out ip))
            {
                Console.WriteLine("Failed to find IP Address");
                ShowUsage();
                return;
            }

            ci.IPAddress = ip.ToString();

            for (int x = 1; x < args.Length; x++)
            {
                var currentArg = args[x].ToLower();
                if (currentArg == "-u" || currentArg == "-user" || currentArg == "-username")
                {
                    ci.Username = args[++x];
                }
                else if (currentArg == "-p" || currentArg == "-password")
                {
                    ci.Password = args[++x];
                }
                else if(currentArg.StartsWith("--"))
                {
                    var c = currentArg.Trim('-');
                    theCommand = (Commands)Enum.Parse(typeof(Commands), c, true);

                    if( theCommand == Commands.Cycle)
                    {
                        index = int.Parse(args[++x]);
                    }

                    if (theCommand == Commands.SetSwitch)
                    {
                        index = int.Parse(args[++x]);
                        desiredState = bool.Parse(args[++x]);
                    }
                }
            }
            Task task = null;
            if( theCommand == Commands.GetStatus)
            {
                task = GetStatus(ci);
            }
            else if (theCommand == Commands.Cycle)
            {
                task = CycleOutlet(ci, index);
            }
            else if (theCommand == Commands.SetSwitch)
            {
                task = SetOutlet(ci, index, desiredState);
            }
            else
            {
                Console.WriteLine("Unknown command");
                ShowUsage();
                return;
            }

            Task.WaitAll(task);
        }

        private static void ShowUsage()
        {
            Console.WriteLine("Usages:");
            Console.WriteLine("<ipaddress> -u <username> -p <password> --GetStatus");
            Console.WriteLine("<ipaddress> -u <username> -p <password> --Cycle 2");
            Console.WriteLine("<ipaddress> -u <username> -p <password> --SetSwitch 3 false");
            Console.WriteLine("<ipaddress> -u <username> -p <password> --SetSwitch 3 true");
        }

        private async static Task GetStatus(ConnectionInfo ci)
        {
            var c = new DLIOutletClient(ci);
            await c.ConnectAsync();
            var switchInfo = await c.GetSwitchInfo();

            Console.WriteLine(switchInfo.ToDetailsString());
        }

        private async static Task CycleOutlet(ConnectionInfo ci, int index)
        {
            var c = new DLIOutletClient(ci);
            await c.ConnectAsync();
            await c.CycleOutlet(index);
        }

        private async static Task SetOutlet(ConnectionInfo ci, int index, bool desiredState)
        {
            var c = new DLIOutletClient(ci);
            await c.ConnectAsync();
            await c.SetOutlet(index, desiredState);
        }
    }
}