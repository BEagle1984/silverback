// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Main.UseCases.Advanced
{
    public class SameProcessUseCase : UseCase
    {
        public SameProcessUseCase() : base("Producer and Consumer in the same process", 70, 1)
        {
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSilverback()
            .UseModel()
            .WithConnectionToKafka();

        protected override void Configure(BusConfigurator configurator, IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetService<ILogger<SameProcessUseCase>>();

            configurator
                .Subscribe((SimpleIntegrationEvent message) => logger.LogInformation($"Received SimpleIntegrationEvent '{message.Content}"))
                .Connect(endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("silverback-examples-events")
                    {
                        Configuration = new KafkaProducerConfig
                        {
                            BootstrapServers = "PLAINTEXT://localhost:9092"
                        }
                    })
                    .AddInbound(new KafkaConsumerEndpoint("silverback-examples-events")
                    {
                        Configuration = new KafkaConsumerConfig
                        {
                            BootstrapServers = "PLAINTEXT://localhost:9092",
                            GroupId = "SameProcessUseCase",
                            AutoOffsetReset = AutoOffsetReset.Earliest
                        }
                    }));
        }

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisher = serviceProvider.GetService<IEventPublisher>();

            await publisher.PublishAsync(new SimpleIntegrationEvent { Content = DateTime.Now.ToString("HH:mm:ss.fff") });
            await publisher.PublishAsync(new SimpleIntegrationEvent { Content = DateTime.Now.ToString("HH:mm:ss.fff") });
            await publisher.PublishAsync(new SimpleIntegrationEvent { Content = DateTime.Now.ToString("HH:mm:ss.fff") });
            
            while (Console.ReadKey(false).Key != ConsoleKey.Escape)
            {
            }

            Console.WriteLine("Canceling...");
        }
    }
}