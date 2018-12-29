// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common;
using Silverback.Examples.Common.Data;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
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
            .AddBus()
            .AddBroker<KafkaBroker>(options => options
                .AddDbOutboundConnector<ExamplesDbContext>()
                .AddDbOutboundWorker<ExamplesDbContext>());

        protected override void Configure(IBrokerEndpointsConfigurationBuilder endpoints, IServiceProvider serviceProvider) => endpoints
            .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("silverback-examples-events")
            {
                Configuration = new Confluent.Kafka.ProducerConfig
                {
                    BootstrapServers = "PLAINTEXT://kafka:9092",
                    ClientId = GetType().FullName
                }
            })
            .Broker.Connect();

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisher = serviceProvider.GetService<IEventPublisher>();
            var dbContext = serviceProvider.GetRequiredService<ExamplesDbContext>();

            await publisher.PublishAsync(new SimpleIntegrationEvent {Content = DateTime.Now.ToString("HH:mm:ss.fff")});

            await dbContext.SaveChangesAsync();
        }

        protected override void PreExecute(IServiceProvider serviceProvider)
        {
            // Setup OutboundWorker to run every 50 milliseconds using a poor-man scheduler
            serviceProvider.GetRequiredService<JobScheduler>().AddJob(
                "OutboundWorker",
                TimeSpan.FromMilliseconds(50),
                s => s.GetRequiredService<OutboundQueueWorker>().ProcessQueue());
        }

        protected override void PostExecute(IServiceProvider serviceProvider)
        {
            // Let the worker run for some time before 
            Thread.Sleep(2000);
        }
    }
}