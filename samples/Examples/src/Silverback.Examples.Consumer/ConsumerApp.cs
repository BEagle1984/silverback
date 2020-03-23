// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common.Data;
using Silverback.Examples.Consumer.Behaviors;
using Silverback.Examples.Consumer.Configuration;
using Silverback.Examples.Consumer.Subscribers;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

namespace Silverback.Examples.Consumer
{
    public class ConsumerApp : ConsumerAppBase
    {
        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSingleton(this)
            .AddLogging()
            // Silverback Core
            .AddSilverback()
            .AsObservable()
            .UseDbContext<ExamplesDbContext>()
            // Message Broker(s)
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddRabbit()
                .AddDbOffsetStoredInboundConnector()
                .AddInboundConnector()
                .AddDbLoggedInboundConnector()
                .AddDbChunkStore())
            .AddEndpointsConfigurator<KafkaEndpointsConfigurator>()
            .AddEndpointsConfigurator<RabbitEndpointsConfigurator>()
            // Subscribers
            .AddScopedSubscriber<SampleEventsSubscriber>()
            .AddScopedSubscriber<MultipleGroupsSubscriber>()
            .AddScopedSubscriber<LegacyMessagesSubscriber>()
            .AddScopedSubscriber<SilverbackEventsSubscriber>()
            .AddScopedSubscriber<KafkaEventsSubscriber>()
            // Behaviors
            .AddScopedBehavior<LogHeadersBehavior>();

        protected override IBrokerCollection Configure(BusConfigurator busConfigurator) => busConfigurator
            .Connect();
    }
}