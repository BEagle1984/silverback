using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Silverback.Examples.Common;
using Silverback.Examples.Common.Consumer;
using Silverback.Examples.Common.Serialization;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Silverback.Messaging.Subscribers;

namespace Silverback.Examples.ConsumerA
{
    public class LegacyConsumerService : ConsumerService
    {
        protected override void ConfigureServices(IServiceCollection services) => services
            .AddBus()
            .AddBroker<FileSystemBroker>(options => options
                .AddInboundConnector())
            .AddScoped<ISubscriber, SubscriberService>();

        protected override void Configure(IBrokerEndpointsConfigurationBuilder endpoints, IServiceProvider serviceProvider)
        {
            ConfigureNLog(serviceProvider);

            endpoints
                .AddInbound(CreateEndpoint("legacy-messages", new LegacyMessageSerializer()))
                .Connect();
        }

        private static FileSystemEndpoint CreateEndpoint(string name, IMessageSerializer messageSerializer = null)
        {
            var endpoint = new FileSystemEndpoint(name, Configuration.FileSystemBrokerBasePath);

            if (messageSerializer != null)
                endpoint.Serializer = messageSerializer;

            return endpoint;
        }

        private static void ConfigureNLog(IServiceProvider serviceProvider)
        {
            serviceProvider.GetRequiredService<ILoggerFactory>()
                .AddNLog(new NLogProviderOptions { CaptureMessageTemplates = true, CaptureMessageProperties = true });
            NLog.LogManager.LoadConfiguration("nlog.config");
        }
    }
}