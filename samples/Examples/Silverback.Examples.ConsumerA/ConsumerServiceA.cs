using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using NLog.Extensions.Logging;
using Silverback.Examples.Common;
using Silverback.Examples.Common.Consumer;
using Silverback.Examples.Common.Data;
using Silverback.Examples.Common.Serialization;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Messaging.Subscribers;

namespace Silverback.Examples.ConsumerA
{
    public class ConsumerServiceA : ConsumerService
    {
        protected override void ConfigureServices(IServiceCollection services) => services
            .AddBus()
            .AddBroker<FileSystemBroker>(options => options
                .AddDbInboundConnector<ExamplesDbContext>()
                .AddInboundConnector())
            .AddScoped<ISubscriber, SubscriberService>();

        protected override void Configure(IBrokerEndpointsConfigurationBuilder endpoints, IServiceProvider serviceProvider)
        {
            ConfigureNLog(serviceProvider);

            endpoints
                .AddInbound(CreateEndpoint("simple-events"))
                .AddInbound(CreateEndpoint("bad-events"), policy => policy
                    .Chain(
                        policy.Retry(2, TimeSpan.FromMilliseconds(500)),
                        policy.Move(CreateEndpoint("bad-events-error"))))
                .AddInbound(CreateEndpoint("custom-serializer-settings-events", GetCustomSerializer()))
                // Special inbounds (not logged)
                .AddInbound<InboundConnector>(CreateEndpoint("legacy-messages", new LegacyMessageSerializer()))
                .Connect();
        }

        private static FileSystemEndpoint CreateEndpoint(string name, IMessageSerializer messageSerializer = null)
        {
            var endpoint = new FileSystemEndpoint(name, Configuration.FileSystemBrokerBasePath);

            if (messageSerializer != null)
                endpoint.Serializer = messageSerializer;

            return endpoint;
        }

        private static JsonMessageSerializer GetCustomSerializer()
        {
            var serializer = new JsonMessageSerializer
            {
                Encoding = MessageEncoding.Unicode
            };

            return serializer;
        }

        private static void ConfigureNLog(IServiceProvider serviceProvider)
        {
            serviceProvider.GetRequiredService<ILoggerFactory>()
                .AddNLog(new NLogProviderOptions { CaptureMessageTemplates = true, CaptureMessageProperties = true });
            NLog.LogManager.LoadConfiguration("nlog.config");
        }
    }
}