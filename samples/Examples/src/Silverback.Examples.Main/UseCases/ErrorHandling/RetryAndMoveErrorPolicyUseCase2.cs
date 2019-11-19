// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;

namespace Silverback.Examples.Main.UseCases.ErrorHandling
{
    public class RetryAndMoveErrorPolicyUseCase2 : UseCase
    {
        public RetryAndMoveErrorPolicyUseCase2() : base("Deserialization error -> retry (x2) -> move to same topic (x2) -> move to another topic", 11, 1)
        {
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSilverback()
            .UseModel()
            .WithConnectionToKafka();

        protected override void Configure(BusConfigurator configurator, IServiceProvider serviceProvider) =>
            configurator.Connect(endpoints => endpoints
                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("silverback-examples-error-events")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092",
                        ClientId = GetType().FullName
                    },
                    Serializer = new BuggySerializer()
                }));

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisher = serviceProvider.GetService<IEventPublisher>();

            await publisher.PublishAsync(new BadIntegrationEvent { Content = DateTime.Now.ToString("HH:mm:ss.fff") });
        }

        private class BuggySerializer : IMessageSerializer
        {
            public byte[] Serialize(object message, MessageHeaderCollection messageHeaders) => new byte[] { 0, 1, 2, 3, 4 };

            public object Deserialize(byte[] message, MessageHeaderCollection messageHeaders)
            {
                throw new NotImplementedException();
            }
        }
    }
}