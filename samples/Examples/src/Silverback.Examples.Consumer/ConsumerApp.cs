// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Examples.Common.Data;
using Silverback.Examples.Consumer.Behaviors;
using Silverback.Examples.Consumer.Configuration;
using Silverback.Examples.Consumer.HostedServices;
using Silverback.Examples.Consumer.Subscribers;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

namespace Silverback.Examples.Consumer
{
    public class ConsumerApp : ConsumerAppBase
    {
        private ConsumersWatcher? _consumersWatcher;

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSingleton(this)
            .AddLogging()

            // Hosted Services
            .AddSingleton<ConsumersWatcher>()

            // Silverback Core
            .AddSilverback()
            .AsObservable()
            .UseDbContext<ExamplesDbContext>()

            // Message Broker(s)
            .WithConnectionToMessageBroker(
                options => options
                    .AddKafka()
                    .AddRabbit()
                    .AddDbOffsetStoredInboundConnector()
                    .AddInboundConnector()
                    .AddDbLoggedInboundConnector()
                    .AddInMemoryChunkStore()
                    .RegisterConfigurator<KafkaEndpointsConfigurator>()
                    .RegisterConfigurator<RabbitEndpointsConfigurator>())
            .WithLogLevels(
                configurator => configurator
                    .SetLogLevel(IntegrationEventIds.MessageSkipped, LogLevel.Critical))

            // Subscribers
            .AddScopedSubscriber<SampleEventsSubscriber>()
            .AddScopedSubscriber<MultipleGroupsSubscriber>()
            .AddScopedSubscriber<LegacyMessagesSubscriber>()
            .AddScopedSubscriber<SilverbackEventsSubscriber>()
            .AddScopedSubscriber<KafkaEventsSubscriber>()
            .AddSingletonSubscriber<LocalEventsSubscriber>()
            .AddTransientSubscriber<BinaryFilesSubscriber>()

            // Behaviors
            .AddScopedBehavior<LogHeadersBehavior>();

        protected override IBrokerCollection Configure(IBusConfigurator busConfigurator)
        {
            _consumersWatcher = ServiceProvider.GetRequiredService<ConsumersWatcher>();
            _consumersWatcher.StartAsync(CancellationToken.None);

            return busConfigurator.Connect();
        }

        protected override void Exit()
        {
            _consumersWatcher?.Dispose();
        }
    }
}
