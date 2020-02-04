// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Main.UseCases.Kafka.Advanced
{
    public class HeadersUseCase : UseCase
    {
        public HeadersUseCase()
        {
            Title = "Custom message headers";
            Description = "An behavior is used to add some extra headers ('generated-by' and 'timestamp')" +
                          " to the messages being published.";
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSilverback()
            .UseModel()
            .WithConnectionToKafka()
            .AddSingletonBehavior<CustomHeadersBehavior>();

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

            await publisher.PublishAsync(new SimpleIntegrationEvent
                { Content = DateTime.Now.ToString("HH:mm:ss.fff") });
        }

        public class CustomHeadersBehavior : IBehavior
        {
            [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
            public async Task<IReadOnlyCollection<object>> Handle(IReadOnlyCollection<object> messages, MessagesHandler next)
            {
                foreach (var message in messages.OfType<IOutboundEnvelope>())
                {
                    message.Headers.Add("generated-by", "silverback");
                    message.Headers.Add("timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                }

                return await next(messages);
            }
        }
    }
}