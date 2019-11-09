// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common.Data;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Main.UseCases.Advanced
{
    public class MultipleOutboundConnectorsUseCase : UseCase
    {
        public MultipleOutboundConnectorsUseCase() : base("Multiple outbound connectors", 20)
        {
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSilverback()
            .UseModel()
            .UseDbContext<ExamplesDbContext>()
            .WithConnectionToKafka(options => options
                .AddOutboundConnector()
                .AddDbOutboundConnector()
                .AddDbOutboundWorker());

        protected override void Configure(BusConfigurator configurator, IServiceProvider serviceProvider) =>
            configurator.Connect(endpoints => endpoints
                .AddOutbound<IntegrationEventA>(CreateEndpoint())
                .AddOutbound<IntegrationEventB, DeferredOutboundConnector>(CreateEndpoint()));

        private KafkaEndpoint CreateEndpoint() =>
            new KafkaProducerEndpoint("silverback-examples-events")
            {
                Configuration = new KafkaProducerConfig
                {
                    BootstrapServers = "PLAINTEXT://localhost:9092",
                    ClientId = GetType().FullName
                }
            };

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisherA = serviceProvider.GetService<IEventPublisher>();
            var publisherB = serviceProvider.GetService<IEventPublisher>();
            var dbContext = serviceProvider.GetRequiredService<ExamplesDbContext>();

            await publisherA.PublishAsync(new IntegrationEventA { Content = "A->" + DateTime.Now.ToString("HH:mm:ss.fff") });
            await publisherB.PublishAsync(new IntegrationEventB { Content = "B->" + DateTime.Now.ToString("HH:mm:ss.fff") });

            await dbContext.SaveChangesAsync();
        }
    }
}