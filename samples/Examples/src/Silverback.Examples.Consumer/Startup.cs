// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Silverback.Diagnostics;
using Silverback.Examples.Common.Data;
using Silverback.Examples.Consumer.Behaviors;
using Silverback.Examples.Consumer.Configuration;
using Silverback.Examples.Consumer.HostedServices;
using Silverback.Examples.Consumer.Subscribers;

namespace Silverback.Examples.Consumer
{
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Invoked by asp.net framework")]
    public class Startup
    {
        protected void ConfigureServices(IServiceCollection services)
        {
            services
                .AddLogging(
                    loggingBuilder => loggingBuilder
                        .SetMinimumLevel(LogLevel.Trace)
                        .AddSerilog())
                        .AddDbContext<ExamplesDbContext>(
                    options => options
                        .UseSqlServer(sqlServerConnectionString))


                // Hosted Services
                .AddHostedService<ConsumersWatcher>()

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
                        .AddInMemoryChunkStore())
                .AddEndpointsConfigurator<KafkaEndpointsConfigurator>()
                .AddEndpointsConfigurator<RabbitEndpointsConfigurator>()
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
        }

        protected void Configure()
        {
            // Nothing to do anymore
        }
    }
}
