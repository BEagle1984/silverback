// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.ErrorHandling
{
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Invoked by test framework")]
    [SuppressMessage("", "CA1822", Justification = "Startup contract")]
    public class RetryAndSkipErrorPolicy2UseCaseStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddKafka())
                .AddEndpoints(
                    endpoints => endpoints
                        .AddOutbound<IIntegrationEvent>(
                            new KafkaProducerEndpoint("silverback-examples-error-events2")
                            {
                                Configuration = new KafkaProducerConfig
                                {
                                    BootstrapServers = "PLAINTEXT://localhost:9092"
                                },
                                Serializer = new BuggySerializer()
                            }));
        }

        public void Configure()
        {
        }

        private class BuggySerializer : IMessageSerializer
        {
            [SuppressMessage("", "SA1011", Justification = "False positive")]
            [SuppressMessage("ReSharper", "ReturnTypeCanBeNotNullable", Justification = "Interface contract")]
            public byte[]? Serialize(
                object? message,
                MessageHeaderCollection messageHeaders,
                MessageSerializationContext context) =>
                new byte[] { 0, 1, 2, 3, 4 };

            [SuppressMessage("", "SA1011", Justification = "False positive")]
            public (object?, Type) Deserialize(
                byte[]? message,
                MessageHeaderCollection messageHeaders,
                MessageSerializationContext context) =>
                throw new NotImplementedException();

            [SuppressMessage("", "SA1011", Justification = "False positive")]
            public Task<byte[]?> SerializeAsync(
                object? message,
                MessageHeaderCollection messageHeaders,
                MessageSerializationContext context) =>
                Task.FromResult(Serialize(message, messageHeaders, context));

            [SuppressMessage("", "SA1011", Justification = "False positive")]
            public Task<(object?, Type)> DeserializeAsync(
                byte[]? message,
                MessageHeaderCollection messageHeaders,
                MessageSerializationContext context) =>
                throw new NotImplementedException();
        }
    }
}
