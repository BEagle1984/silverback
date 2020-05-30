// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    public class ProduceEmptyMessageUseCase : UseCase
    {
        public ProduceEmptyMessageUseCase()
        {
            Title = "Produce a message without a body";
            Description = "Publish a message with an empty body to a topic.";
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSilverback()
            .UseModel()
            .WithConnectionToMessageBroker(options => options.AddKafka());

        protected override void Configure(IBusConfigurator configurator, IServiceProvider serviceProvider) =>
            configurator.Connect();

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var producer = serviceProvider.GetService<IBroker>().GetProducer(
                new KafkaProducerEndpoint("silverback-examples-events")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092"
                    }
                });

            await producer.ProduceAsync(
                null,
                new MessageHeader[]
                {
                    new MessageHeader("use-case", "empty-message"),
                });
        }
    }
}