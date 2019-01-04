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

namespace Silverback.Examples.Main.UseCases.ErrorHandling
{
    public class RetryAndMoveErrorPolicyUseCase : UseCase
    {
        public RetryAndMoveErrorPolicyUseCase() : base("Retry then move to dead letter", 10)
        {
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddBus()
            .AddBroker<KafkaBroker>();

        protected override void Configure(IBrokerEndpointsConfigurationBuilder endpoints, IServiceProvider serviceProvider) => endpoints
            .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("silverback-examples-bad-events")
            {
                Configuration = new KafkaProducerConfig
                {
                    BootstrapServers = "PLAINTEXT://kafka:9092",
                    ClientId = GetType().FullName
                }
            })
            .Broker.Connect();

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisher = serviceProvider.GetService<IEventPublisher>();

            await publisher.PublishAsync(new BadIntegrationEvent { Content = DateTime.Now.ToString("HH:mm:ss.fff") });
        }
    }
}