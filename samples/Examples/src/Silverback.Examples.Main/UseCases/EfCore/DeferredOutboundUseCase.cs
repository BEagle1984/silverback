// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common.Data;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Main.UseCases.EfCore
{
    public class DeferredOutboundUseCase : UseCase
    {
        public DeferredOutboundUseCase() : base("Deferred publish (DbOutbound)", 10)
        {
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSilverback()
            .UseModel()
            .UseDbContext<ExamplesDbContext>()
            .WithConnectionTo<KafkaBroker>(options => options
                .AddDbOutboundConnector()
                .AddDbOutboundWorker())
            .AddSingletonBehavior<CustomHeadersBehavior>();

        protected override void Configure(BusConfigurator configurator, IServiceProvider serviceProvider) =>
            configurator.Connect(endpoints => endpoints
                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("silverback-examples-events")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092",
                        ClientId = GetType().FullName
                    }
                }));

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisher = serviceProvider.GetService<IEventPublisher>();

            await publisher.PublishAsync(new SimpleIntegrationEvent {Content = DateTime.Now.ToString("HH:mm:ss.fff")});

            var dbContext = serviceProvider.GetRequiredService<ExamplesDbContext>();
            await dbContext.SaveChangesAsync();
        }
        
        public class CustomHeadersBehavior : IBehavior
        {
            public async Task<IEnumerable<object>> Handle(IEnumerable<object> messages, MessagesHandler next)
            {
                foreach (var message in messages.OfType<IOutboundMessage>())
                {
                    message.Headers.Add("was-created", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                }

                return await next(messages);
            }
        }
    }
}