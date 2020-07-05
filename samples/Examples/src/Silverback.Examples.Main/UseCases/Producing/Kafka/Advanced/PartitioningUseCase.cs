// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    public class PartitioningUseCase : UseCase
    {
        public PartitioningUseCase()
        {
            Title = "Kafka partitioning (Kafka key)";
            Description = "Silverback allows to decorate some message properties with the [KafkaKeyMember] attribute " +
                          "to be used to generate a key for the messages being sent. " +
                          "When a key is specified it will be used to ensure that all messages with the same key land " +
                          "in the same partition (this is very important because the message ordering can be enforced " +
                          "inside the same partition only and it is also a prerequisite for the compacted topics).";
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
                        }));

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisher = serviceProvider.GetService<IEventPublisher>();

            await publisher.PublishAsync(
                new PartitionedSimpleIntegrationEvent
                {
                    Key = "AAAAAAAAAA", Content = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)
                });
            await publisher.PublishAsync(
                new PartitionedSimpleIntegrationEvent
                {
                    Key = "zzzzzzzzzz", Content = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)
                });
            await publisher.PublishAsync(
                new PartitionedSimpleIntegrationEvent
                {
                    Key = "0000000000", Content = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)
                });
        }
    }
}
