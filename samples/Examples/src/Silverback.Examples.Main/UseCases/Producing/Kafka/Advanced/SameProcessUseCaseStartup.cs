// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Invoked by test framework")]
    [SuppressMessage("", "CA1822", Justification = "Startup contract")]
    public class SameProcessUseCaseStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddKafka())
                .AddDelegateSubscriber(
                    (SimpleIntegrationEvent message, IServiceProvider serviceProvider) =>
                    {
                        var logger = serviceProvider.GetRequiredService<ILogger>();
                        logger.LogInformation($"Received SimpleIntegrationEvent '{message.Content}'");
                    })
                .AddEndpoints(
                    endpoints => endpoints
                        .AddOutbound<IIntegrationEvent>(
                            new KafkaProducerEndpoint("silverback-examples-events-sp")
                            {
                                Configuration = new KafkaProducerConfig
                                {
                                    BootstrapServers = "PLAINTEXT://localhost:9092"
                                }
                            })
                        .AddInbound(
                            new KafkaConsumerEndpoint("silverback-examples-events-sp")
                            {
                                Configuration = new KafkaConsumerConfig
                                {
                                    BootstrapServers = "PLAINTEXT://localhost:9092",
                                    GroupId = "same-process-uc",
                                    AutoOffsetReset = AutoOffsetReset.Earliest
                                }
                            }));
        }

        public void Configure()
        {
        }
    }
}
