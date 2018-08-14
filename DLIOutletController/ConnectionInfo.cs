using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace DLIOutletController
{
    public class ConnectionInfo
    {
        public string IPAddress { get; set; }
        public int Port { get; set; } = 80;
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
