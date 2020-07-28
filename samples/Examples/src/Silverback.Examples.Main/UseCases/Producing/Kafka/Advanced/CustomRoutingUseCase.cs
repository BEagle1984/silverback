// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    public class CustomRoutingUseCase : UseCase
    {
        public CustomRoutingUseCase()
        {
            Title = "Dynamic custom routing";
            Description = "In this example a custom OutboundRouter is used to " +
                          "send urgent message to an additional topic.";
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSilverback()
            .UseModel()
            .WithConnectionToMessageBroker(options => options.AddKafka())
            .AddScopedOutboundRouter<PrioritizedOutboundRouter>();

        protected override void Configure(IBusConfigurator configurator, IServiceProvider serviceProvider) =>
            configurator.Connect(
                endpoints => endpoints
                    .AddOutbound<SimpleIntegrationEvent, PrioritizedOutboundRouter>());

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisher = serviceProvider.GetService<IEventPublisher>();

            await publisher.PublishAsync(new SimpleIntegrationEvent { Content = "Low priority" });
            await publisher.PublishAsync(new SimpleIntegrationEvent { Content = "URGENT" });
        }

        [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Instantiated by Silverback")]
        [SuppressMessage("ReSharper", "CA1812", Justification = "Instantiated by Silverback")]
        private class PrioritizedOutboundRouter : OutboundRouter<SimpleIntegrationEvent>
        {
            private static readonly IProducerEndpoint NormalEndpoint =
                new KafkaProducerEndpoint("silverback-examples-events")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092"
                    }
                };

            private static readonly IProducerEndpoint HighPriorityEndpoint =
                new KafkaProducerEndpoint("silverback-examples-events-2")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092"
                    }
                };

            public override IEnumerable<IProducerEndpoint> Endpoints
            {
                get
                {
                    yield return NormalEndpoint;
                    yield return HighPriorityEndpoint;
                }
            }

            public override IEnumerable<IProducerEndpoint> GetDestinationEndpoints(
                SimpleIntegrationEvent message,
                MessageHeaderCollection headers)
            {
                if (message.Content == "URGENT")
                    yield return HighPriorityEndpoint;

                yield return NormalEndpoint;
            }
        }
    }
}
