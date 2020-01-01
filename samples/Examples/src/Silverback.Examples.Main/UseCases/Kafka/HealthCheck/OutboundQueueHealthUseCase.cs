// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common;
using Silverback.Examples.Common.Data;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.HealthChecks;
using Silverback.Messaging.Messages;

namespace Silverback.Examples.Main.UseCases.Kafka.HealthCheck
{
    public class OutboundQueueHealthUseCase : UseCase
    {
        public OutboundQueueHealthUseCase()
        {
            Title = "Check outbound queue";
            Description = "Check that the outbox queue length doesn't exceed the configured threshold and that " +
                          "the messages aren't older than the configure max age.";
            ExecutionsCount = 1;
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSilverback()
                .UseModel()
                .UseDbContext<ExamplesDbContext>()
                .WithConnectionToKafka(options => options
                    .AddDbOutboundConnector()
                    .AddDbOutboundWorker());
        }

        protected override void Configure(BusConfigurator configurator, IServiceProvider serviceProvider)
        {
            configurator.Connect(endpoints => endpoints
                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("silverback-examples-events")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092",
                        MessageTimeoutMs = 1000
                    }
                })
                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("silverback-examples-events-two")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092",
                        MessageTimeoutMs = 1000
                    }
                })
                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("silverback-examples-failure")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://somwhere:1000",
                        MessageTimeoutMs = 1000
                    }
                }));
        }

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            Console.ForegroundColor = Constants.PrimaryColor;
            Console.WriteLine("Checking outbound queue (maxAge: 100ms, maxQueueLength: 1)...");
            ConsoleHelper.ResetColor();

            var result = await new OutboundQueueHealthCheckService(
                    serviceProvider.GetRequiredService<IOutboundQueueConsumer>())
                .CheckIsHealthy(maxAge: TimeSpan.FromMilliseconds(100), maxQueueLength: 1);

            Console.ForegroundColor = Constants.PrimaryColor;
            Console.WriteLine($"Healthy: {result}");
            ConsoleHelper.ResetColor();
        }
    }
}