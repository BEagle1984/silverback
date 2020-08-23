// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common;
using Silverback.Examples.Common.Data;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.HealthCheck
{
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Invoked by test framework")]
    [SuppressMessage("", "CA1822", Justification = "Startup contract")]
    public class OutboundQueueHealthUseCaseStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddDbContext<ExamplesDbContext>(
                    options => options
                        .UseSqlServer(SqlServerConnectionHelper.GetProducerConnectionString()))
                .AddSilverback()
                .UseModel()
                .UseDbContext<ExamplesDbContext>()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddKafka()
                        .AddDbOutboundConnector()
                        .AddDbOutboundWorker())
                .AddEndpoints(
                    endpoints => endpoints
                        .AddOutbound<IIntegrationEvent>(
                            new KafkaProducerEndpoint("silverback-examples-events")
                            {
                                Configuration = new KafkaProducerConfig
                                {
                                    BootstrapServers = "PLAINTEXT://localhost:9092",
                                    MessageTimeoutMs = 1000
                                }
                            })
                        .AddOutbound<IIntegrationEvent>(
                            new KafkaProducerEndpoint("silverback-examples-events-two")
                            {
                                Configuration = new KafkaProducerConfig
                                {
                                    BootstrapServers = "PLAINTEXT://localhost:9092",
                                    MessageTimeoutMs = 1000
                                }
                            })
                        .AddOutbound<IIntegrationEvent>(
                            new KafkaProducerEndpoint("silverback-examples-failure")
                            {
                                Configuration = new KafkaProducerConfig
                                {
                                    BootstrapServers = "PLAINTEXT://somwhere:1000",
                                    MessageTimeoutMs = 1000
                                }
                            }));

            services
                .AddHealthChecks()
                .AddOutboundQueueCheck();
        }

        public void Configure(ExamplesDbContext dbContext)
        {
            dbContext.Database.EnsureCreated();
        }
    }
}
