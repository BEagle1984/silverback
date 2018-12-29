// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;

namespace Silverback.Examples.Main.UseCases.Basic
{
    public class TranslateUseCase : UseCase, ISubscriber
    {
        public TranslateUseCase() : base("Translate outbound message", 20)
        {
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddBus()
            .AddBroker<KafkaBroker>()
            .AddScoped<ISubscriber, TranslateUseCase>();

        protected override void Configure(IBrokerEndpointsConfigurationBuilder endpoints, IServiceProvider serviceProvider) => endpoints
            .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("silverback-examples-events")
            {
                Configuration = new Confluent.Kafka.ProducerConfig
                {
                    BootstrapServers = "PLAINTEXT://kafka:9092",
                    ClientId = GetType().FullName
                }
            });

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisher = serviceProvider.GetService<IEventPublisher>();

            await publisher.PublishAsync(new SimpleEvent { Content = DateTime.Now.ToString("HH:mm:ss.fff") });
        }

        [Subscribe]
        IMessage OnSimpleEvent(SimpleEvent message) => new SimpleIntegrationEvent {Content = message.Content};
    }
}