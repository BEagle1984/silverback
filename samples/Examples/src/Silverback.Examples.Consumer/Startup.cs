// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Examples.Common;
using Silverback.Examples.Common.Data;
using Silverback.Examples.Consumer.Behaviors;
using Silverback.Examples.Consumer.Configuration;
using Silverback.Examples.Consumer.HostedServices;
using Silverback.Examples.Consumer.Subscribers;

namespace Silverback.Examples.Consumer
{
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Invoked by asp.net framework")]
    [SuppressMessage("", "CA1822", Justification = "Startup contract")]
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddOptions()
                .AddDbContext<ExamplesDbContext>(
                    options => options
                        .UseSqlServer(SqlServerConnectionHelper.GetConsumerConnectionString()))

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
                        .SetLogLevel(IntegrationEventIds.MessageSkipped, LogLevel.Critical)
                        .SetLogLevel(
                            KafkaEventIds.KafkaExceptionAutoRecovery,
                            (exception, logLevel, _) =>
                                exception.Message.Contains("Unknown topic", StringComparison.Ordinal)
                                    ? LogLevel.Trace
                                    : logLevel))

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

        public void Configure(ExamplesDbContext dbContext)
        {
            dbContext.Database.EnsureCreated();
        }
    }
}
