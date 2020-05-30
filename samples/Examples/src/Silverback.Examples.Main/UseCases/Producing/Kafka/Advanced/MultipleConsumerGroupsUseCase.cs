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
    public class MultipleConsumerGroupsUseCase : UseCase
    {
        public MultipleConsumerGroupsUseCase()
        {
            Title = "Multiple consumer groups in the same process";
            Description =
                "The messages are published in the simplest way possible but in the consumer app there will be" +
                "multiple consumers listening to the message.";
            ExecutionsCount = 1;
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSilverback()
            .UseModel()
            .WithConnectionToMessageBroker(options => options.AddKafka());

        protected override void Configure(IBusConfigurator configurator, IServiceProvider serviceProvider)
        {
            configurator.Connect(endpoints => endpoints
                .AddOutbound<IIntegrationMessage>(new KafkaProducerEndpoint("silverback-examples-multiple-groups")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092"
                    }
                }));
        }

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisher = serviceProvider.GetService<IPublisher>();

            await publisher.PublishAsync(new MultipleGroupsMessage() { Content = "first" });
            await publisher.PublishAsync(new MultipleGroupsMessage { Content = "second" });
            await publisher.PublishAsync(new MultipleGroupsMessage { Content = "third" });
        }
    }
}