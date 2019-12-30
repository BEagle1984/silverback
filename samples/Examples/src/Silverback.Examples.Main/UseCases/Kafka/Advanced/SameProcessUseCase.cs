// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Main.UseCases.Kafka.Advanced
{
    public class SameProcessUseCase : UseCase
    {
        public SameProcessUseCase()
        {
            Title = "Producer and Consumer in the same process";
            Description = "Sometimes you want to use Kafka only to asynchronously process some work load but the " +
                          "producer and consumer reside in the same process (same microservice). This has been very " +
                          "tricky with the earlier versions of Silverback but it now works naturally. " +
                          "Note: An inbound endpoint is added only to demonstrate that the feared 'mortal loop' is " +
                          "not an issue anymore.";
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
                    .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("silverback-examples-events-sp")
                    {
                        Configuration = new KafkaProducerConfig
                        {
                            BootstrapServers = "PLAINTEXT://localhost:9092"
                        }
                    })
                    .AddInbound(new KafkaConsumerEndpoint("silverback-examples-events-sp")
                    {
                        Configuration = new KafkaConsumerConfig
                        {
                            BootstrapServers = "PLAINTEXT://localhost:9092",
                            GroupId = "same-process-uc",
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

            Console.WriteLine("Waiting for the messages to be consumed (press ESC to abort)...");
            
            while (Console.ReadKey(false).Key != ConsoleKey.Escape)
            {
            }
        }
    }
}