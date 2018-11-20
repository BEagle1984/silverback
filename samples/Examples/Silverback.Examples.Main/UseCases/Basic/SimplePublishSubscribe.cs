using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Main.UseCases.Basic
{
    public class SimplePublishSubscribe : UseCase
    {
        public SimplePublishSubscribe() : base("Simple publish subscribe", 100)
        {
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddBus()
            .AddBroker<FileSystemBroker>(options => options
                .AddOutboundConnector());

        protected override void Configure(IBrokerEndpointsConfigurationBuilder endpoints) => endpoints
            .AddOutbound<IIntegrationEvent>(FileSystemEndpoint.Create("simple-events", Configuration.FileSystemBrokerBasePath));

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisher = serviceProvider.GetService<IEventPublisher<SimpleIntegrationEvent>>();

            await publisher.PublishAsync(new SimpleIntegrationEvent {Content = DateTime.Now.ToString("HH:mm:ss.fff")});
        }
    }
}