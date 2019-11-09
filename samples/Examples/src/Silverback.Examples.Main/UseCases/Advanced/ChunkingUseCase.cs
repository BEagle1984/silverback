// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Main.UseCases.Advanced
{
    public class ChunkingUseCase : UseCase
    {
        public ChunkingUseCase() : base("Message chunking", 40)
        {
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSilverback()
            .UseModel()
            .WithConnectionToKafka(options => options
                .AddOutboundConnector());

        protected override void Configure(BusConfigurator configurator, IServiceProvider serviceProvider) =>
            configurator.Connect(endpoints => endpoints
                .AddOutbound<IIntegrationEvent>(CreateEndpoint()));

        private KafkaEndpoint CreateEndpoint() =>
            new KafkaProducerEndpoint("silverback-examples-events")
            {
                Configuration = new KafkaProducerConfig
                {
                    BootstrapServers = "PLAINTEXT://localhost:9092",
                    ClientId = GetType().FullName
                },
                Chunk = new ChunkSettings
                {
                    Size = 50
                }
            };

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisher = serviceProvider.GetService<IEventPublisher>();

            await publisher.PublishAsync(new SimpleIntegrationEvent { Content = DateTime.Now.ToString("HH:mm:ss.fff") });
        }
    }
}