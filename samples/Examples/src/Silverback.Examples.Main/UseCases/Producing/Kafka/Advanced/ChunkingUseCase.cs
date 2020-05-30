// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    public class ChunkingUseCase : UseCase
    {
        public ChunkingUseCase()
        {
            Title = "Message chunking";
            Description = "The messages are split into smaller messages (50 bytes each in this example) and " +
                          "transparently rebuilt on the consumer end to be consumed like usual.";
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSilverback()
            .UseModel()
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddOutboundConnector());

        protected override void Configure(IBusConfigurator configurator, IServiceProvider serviceProvider) =>
            configurator.Connect(endpoints => endpoints
                .AddOutbound<IIntegrationEvent>(
                    new KafkaProducerEndpoint("silverback-examples-events-chunked")
                    {
                        Configuration = new KafkaProducerConfig
                        {
                            BootstrapServers = "PLAINTEXT://localhost:9092"
                        },
                        Chunk = new ChunkSettings
                        {
                            Size = 50
                        }
                    }));

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisher = serviceProvider.GetService<IEventPublisher>();

            await publisher.PublishAsync(new SimpleIntegrationEvent
                { Content = DateTime.Now.ToString("HH:mm:ss.fff") });
        }
    }
}