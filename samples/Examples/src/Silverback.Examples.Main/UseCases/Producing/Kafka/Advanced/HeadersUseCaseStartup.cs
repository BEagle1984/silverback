// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Invoked by test framework")]
    [SuppressMessage("", "CA1822", Justification = "Startup contract")]
    public class HeadersUseCaseStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddKafka())
                .AddSingletonBehavior<CustomHeadersBehavior>()
                .AddEndpoints(
                    endpoints => endpoints
                        .AddOutbound<IIntegrationEvent>(
                            new KafkaProducerEndpoint("silverback-examples-events")
                            {
                                Configuration = new KafkaProducerConfig
                                {
                                    BootstrapServers = "PLAINTEXT://localhost:9092"
                                }
                            }));
        }

        public void Configure()
        {
        }

        [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Instantiated by Silverback")]
        [SuppressMessage("", "CA1812", Justification = "Instantiated by Silverback")]
        private class CustomHeadersBehavior : IBehavior
        {
            public async Task<IReadOnlyCollection<object>> Handle(
                IReadOnlyCollection<object> messages,
                MessagesHandler next)
            {
                foreach (var message in messages.OfType<IOutboundEnvelope>())
                {
                    message.Headers.Add("generated-by", "silverback");
                }

                return await next(messages);
            }
        }
    }
}
