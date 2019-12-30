// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Main.UseCases.Kafka.Advanced
{
    public class BatchProcessingUseCase : UseCase
    {
        public BatchProcessingUseCase()
        {
            Title = "Batch processing (w/ multiple consumer threads)";
            Description = "Fetching and processing the messages in batch of multiple messages (5 in our case) " +
                          "to avoid micro-transactions when processing huge streams. The inbound connector is also " +
                          "configured to use 2 concurrent threads (2 instances of the Confluent.Kafka consumer).";
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSilverback()
            .UseModel()
            .WithConnectionToKafka(options => options
                .AddOutboundConnector());

        protected override void Configure(BusConfigurator configurator, IServiceProvider serviceProvider) =>
            configurator.Connect(endpoints => endpoints
                .AddOutbound<SampleBatchProcessedEvent>(CreateEndpoint()));

        private KafkaProducerEndpoint CreateEndpoint() =>
            new KafkaProducerEndpoint("silverback-examples-batch")
            {
                Configuration = new KafkaProducerConfig
                {
                    BootstrapServers = "PLAINTEXT://localhost:9092"
                }
            };

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisher = serviceProvider.GetService<IEventPublisher>();

            for (int i = 0; i < 22; i++)
            {
                await publisher.PublishAsync(new SampleBatchProcessedEvent
                {
                    Content = (i + 1) + " -" + DateTime.Now.ToString("HH:mm:ss.fff")
                });
            }
        }
    }
}