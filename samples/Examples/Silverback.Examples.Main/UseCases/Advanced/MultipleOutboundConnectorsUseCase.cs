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

namespace Silverback.Examples.Main.UseCases.Advanced
{
    // TODO: Implement
    public class MultipleOutboundConnectorsUseCase : UseCase
    {
        public MultipleOutboundConnectorsUseCase() : base("Multiple outbound connectors", 20)
        {
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddBus()
            .AddBroker<KafkaBroker>(options => options
                .AddOutboundConnector()
                .AddDbOutboundConnector<ExamplesDbContext>()
                .AddDbOutboundWorker<ExamplesDbContext>());

        protected override void Configure(IBrokerEndpointsConfigurationBuilder endpoints) => endpoints
            .AddOutbound<IntegrationEventA>(CreateEndpoint())
            .AddOutbound<IntegrationEventB, DeferredOutboundConnector>(CreateEndpoint())
            ;

        private KafkaEndpoint CreateEndpoint() =>
            new KafkaEndpoint("silverback-examples-events")
            {
                Configuration = new KafkaConfigurationDictionary
                {
                    {"bootstrap.servers", "PLAINTEXT://kafka:9092"},
                    {"client.id", GetType().FullName}
                }
            };

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisherA = serviceProvider.GetService<IEventPublisher<IntegrationEventA>>();
            var publisherB = serviceProvider.GetService<IEventPublisher<IntegrationEventB>>();
            var dbContext = serviceProvider.GetRequiredService<ExamplesDbContext>();

            await publisherA.PublishAsync(new IntegrationEventA { Content = "A->" + DateTime.Now.ToString("HH:mm:ss.fff") });
            await publisherB.PublishAsync(new IntegrationEventB { Content = "B->" + DateTime.Now.ToString("HH:mm:ss.fff") });

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