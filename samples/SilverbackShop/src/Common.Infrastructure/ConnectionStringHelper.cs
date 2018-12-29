namespace SilverbackShop.Common.Infrastructure
{
    public static class ConnectionStringHelper
    {
        public static string SetServerName(this string connectionString)
            => connectionString.Replace("{server}",
                DockerHelper.RunningInDocker
                    ? DockerHelper.DockerHostMachineIpAddress
                    : "127.0.0.1");
    }
}