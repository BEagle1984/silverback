using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SilverbackShop.Common.Infrastructure
{
    public static class DockerHelper
    {
        public static bool RunningInDocker => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
        public static string DockerHostMachineIpAddress => Dns.GetHostAddresses(new Uri("http://docker.for.win.localhost").Host)[0].ToString();
    }
}
