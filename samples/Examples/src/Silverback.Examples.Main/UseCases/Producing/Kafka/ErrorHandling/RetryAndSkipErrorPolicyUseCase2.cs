// Copyright (c) 2020 Sergio Aquilini
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

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.ErrorHandling
{
    public class RetryAndSkipErrorPolicyUseCase2 : UseCase
    {
        public RetryAndSkipErrorPolicyUseCase2()
        {
            Title = "Simulate a deserialization error (Retry + Skip)";
            Description = "The consumer will retry to process the message (x2) and " +
                          "finally skip it.";
            ExecutionsCount = 1;
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSilverback()
            .UseModel()
            .WithConnectionToMessageBroker(options => options.AddKafka());

        protected override void Configure(BusConfigurator configurator, IServiceProvider serviceProvider) =>
            configurator.Connect(endpoints => endpoints
                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("silverback-examples-error-events2")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092"
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
            public byte[] Serialize(
                object message,
                MessageHeaderCollection messageHeaders,
                MessageSerializationContext context) =>
                new byte[] { 0, 1, 2, 3, 4 };

            public object Deserialize(
                byte[] message,
                MessageHeaderCollection messageHeaders,
                MessageSerializationContext context) =>
                throw new NotImplementedException();

            public Task<byte[]> SerializeAsync(
                object message,
                MessageHeaderCollection messageHeaders,
                MessageSerializationContext context) =>
                Task.FromResult(Serialize(message, messageHeaders, context));

            public Task<object> DeserializeAsync(
                byte[] message,
                MessageHeaderCollection messageHeaders,
                MessageSerializationContext context) =>
                throw new NotImplementedException();
        }
    }
}