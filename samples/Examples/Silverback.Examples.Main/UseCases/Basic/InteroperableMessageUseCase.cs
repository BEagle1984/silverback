using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common;
using Silverback.Examples.Common.Messages;
using Silverback.Examples.Common.Serialization;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;

namespace Silverback.Examples.Main.UseCases.Basic
{
    public class InteroperableMessageUseCase : UseCase
    {
        public InteroperableMessageUseCase() : base("Interoperable incoming message (free schema, not published by Silberback)", 40)
        {
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddBus()
            .AddBroker<FileSystemBroker>();

        protected override void Configure(IBrokerEndpointsConfigurationBuilder endpoints) { }

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var broker = serviceProvider.GetRequiredService<IBroker>();
            await broker.GetProducer(CreateEndpoint("legacy-messages"))
                .ProduceAsync(new LegacyMessage { Content = "LEGACY - " + DateTime.Now.ToString("HH:mm:ss.fff") });
        }

        private IEndpoint CreateEndpoint(string name) =>
            new FileSystemEndpoint(name, Configuration.FileSystemBrokerBasePath)
            {
                Serializer = new LegacyMessageSerializer()
            };
    }
}