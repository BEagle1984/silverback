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

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    public class HeadersUseCase : UseCase
    {
        public HeadersUseCase()
        {
            Title = "Custom message headers";
            Description = "The Timestamp property of the message is published as header. Additionally a behavior is " +
                          "used to add an extra 'generated-by' header.";
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSilverback()
            .UseModel()
            .WithConnectionToMessageBroker(options => options.AddKafka())
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

            await publisher.PublishAsync(new EventWithHeaders
            {
                Content = DateTime.Now.ToString("HH:mm:ss.fff"),
                StringHeader = "hello!",
                IntHeader = 42,
                BoolHeader = false,
            });
        }

        public class CustomHeadersBehavior : IBehavior
        {
            [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
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