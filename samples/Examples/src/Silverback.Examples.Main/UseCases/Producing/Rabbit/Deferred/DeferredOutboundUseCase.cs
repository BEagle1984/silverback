// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Silverback.Examples.Common.Data;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Main.UseCases.Producing.Rabbit.Deferred
{
    public class DeferredOutboundUseCase : UseCase
    {
        public DeferredOutboundUseCase()
        {
            Title = "Deferred publish (DbOutbound)";
            Description = "The messages are stored into an outbox table and asynchronously published. " +
                          "An outbound worker have to be started to process the outbox table.";
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSilverback()
            .UseModel()
            .UseDbContext<ExamplesDbContext>()
            .WithConnectionToMessageBroker(
                options => options
                    .AddRabbit()
                    .AddDbOutboundConnector()
                    .AddDbOutboundWorker())
            .AddSingletonBehavior<CustomHeadersBehavior>();

        protected override void Configure(IBusConfigurator configurator, IServiceProvider serviceProvider) =>
            configurator.Connect(
                endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(
                        new RabbitExchangeProducerEndpoint("silverback-examples-events-fanout")
                        {
                            Exchange = new RabbitExchangeConfig
                            {
                                IsDurable = true,
                                IsAutoDeleteEnabled = false,
                                ExchangeType = ExchangeType.Fanout
                            },
                            Connection = new RabbitConnectionConfig
                            {
                                HostName = "localhost",
                                UserName = "guest",
                                Password = "guest"
                            }
                        }));

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisher = serviceProvider.GetService<IEventPublisher>();

            await publisher.PublishAsync(
                new SimpleIntegrationEvent
                {
                    Content = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)
                });

            var dbContext = serviceProvider.GetRequiredService<ExamplesDbContext>();
            await dbContext.SaveChangesAsync();
        }

        [SuppressMessage("ReSharper", "CA1034", Justification = "Use case isolation")]
        public class CustomHeadersBehavior : IBehavior
        {
            public async Task<IReadOnlyCollection<object>> Handle(
                IReadOnlyCollection<object> messages,
                MessagesHandler next)
            {
                foreach (var message in messages.OfType<IOutboundEnvelope>())
                {
                    message.Headers.Add(
                        "was-created",
                        DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
                }

                return await next(messages);
            }
        }
    }
}
