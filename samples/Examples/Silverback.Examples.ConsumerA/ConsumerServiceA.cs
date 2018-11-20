using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common;
using Silverback.Examples.Common.Consumer;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Examples.ConsumerA
{
    public class ConsumerServiceA : ConsumerService
    {
        static void Main(string[] args)
        {
            new ConsumerServiceA().Init();
        }
        protected override void ConfigureServices(IServiceCollection services) => services
            .AddBus()
            .AddBroker<FileSystemBroker>(options => options
                .AddOutboundConnector())
            .AddScoped<ISubscriber, SubscriberService>();

        protected override void Configure(IBrokerEndpointsConfigurationBuilder endpoints) => endpoints
            .AddInbound(CreateEndpoint("simple-events"))
            .AddInbound(CreateEndpoint("bad-events"), policy => policy
            .Chain(policy.Retry(2, TimeSpan.FromMilliseconds(500)), policy.Move(CreateEndpoint("bad-events-error"))))
            .Connect();

        private static FileSystemEndpoint CreateEndpoint(string name) => FileSystemEndpoint.Create(name, Configuration.FileSystemBrokerBasePath);
    }
}
