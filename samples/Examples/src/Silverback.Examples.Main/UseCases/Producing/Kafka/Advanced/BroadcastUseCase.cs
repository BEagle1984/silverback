// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    public class BroadcastUseCase : UseCase
    {
        public BroadcastUseCase()
        {
            Title = "Multiple destination endpoints";
            Description = "The same message is produced to multiple endpoints.";
            ExecutionsCount = 1;
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSilverback()
            .UseModel()
            .WithConnectionToMessageBroker(options => options.AddKafka());

        protected override void Configure(IBusConfigurator configurator, IServiceProvider serviceProvider) =>
            configurator.Connect(
                endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(
                        new KafkaProducerEndpoint("silverback-examples-events")
                        {
                            Configuration = new KafkaProducerConfig
                            {
                                BootstrapServers = "PLAINTEXT://localhost:9092"
                            }
                        },
                        new KafkaProducerEndpoint("silverback-examples-events-2")
                        {
                            Configuration = new KafkaProducerConfig
                            {
                                BootstrapServers = "PLAINTEXT://localhost:9092"
                            }
                        })
                    .AddOutbound<SimpleIntegrationEvent>(
                        new KafkaProducerEndpoint("silverback-examples-events-3")
                        {
                            Configuration = new KafkaProducerConfig
                            {
                                BootstrapServers = "PLAINTEXT://localhost:9092"
                            }
                        }));

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisher = serviceProvider.GetService<IEventPublisher>();

            await publisher.PublishAsync(
                new SimpleIntegrationEvent
                    { Content = Guid.NewGuid().ToString("N") });
        }
    }
}
