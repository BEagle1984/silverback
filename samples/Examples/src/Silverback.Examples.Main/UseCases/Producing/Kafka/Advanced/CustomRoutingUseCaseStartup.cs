// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Messages;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Invoked by test framework")]
    [SuppressMessage("", "CA1822", Justification = "Startup contract")]
    public class CustomRoutingUseCaseStartup : UseCase
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddKafka())
                .AddScopedOutboundRouter<PrioritizedOutboundRouter>()
                .AddEndpoints(
                    endpoints => endpoints
                        .AddOutbound<SimpleIntegrationEvent, PrioritizedOutboundRouter>());
        }

        public void Configure()
        {
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
