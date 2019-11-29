// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Main.UseCases.Advanced
{
    public class PartitioningUseCase : UseCase
    {
        public PartitioningUseCase() : base("Kafka partitioning (partition key)", 60)
        {
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSilverback()
            .UseModel()
            .WithConnectionToKafka();

        protected override void Configure(BusConfigurator configurator, IServiceProvider serviceProvider) =>
            configurator.Connect(endpoints => endpoints
                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("silverback-examples-events")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092"
                    }
                }));

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisher = serviceProvider.GetService<IEventPublisher>();

            await publisher.PublishAsync(new PartitionedSimpleIntegrationEvent { Key = "AAAAAAAAAA", Content = DateTime.Now.ToString("HH:mm:ss.fff") });
            await publisher.PublishAsync(new PartitionedSimpleIntegrationEvent { Key = "zzzzzzzzzz", Content = DateTime.Now.ToString("HH:mm:ss.fff") });
            await publisher.PublishAsync(new PartitionedSimpleIntegrationEvent { Key = "0000000000", Content = DateTime.Now.ToString("HH:mm:ss.fff") });
        }
    }
}