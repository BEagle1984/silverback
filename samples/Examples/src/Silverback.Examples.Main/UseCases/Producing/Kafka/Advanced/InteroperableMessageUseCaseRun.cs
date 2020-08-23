// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Silverback.Examples.Common.Messages;
using Silverback.Examples.Main.Menu;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    [SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Invoked by test framework")]
    public class InteroperableMessageUseCaseRun : IAsyncRunnable
    {
        private readonly IBroker _broker;

        public InteroperableMessageUseCaseRun(IBroker broker)
        {
            _broker = broker;
        }

        public async Task Run()
        {
            var producer = _broker.GetProducer(
                new KafkaProducerEndpoint("silverback-examples-legacy-messages")
                {
                    Serializer = new LegacyMessageSerializer(),
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092"
                    }
                });

            await producer.ProduceAsync(
                new LegacyMessage
                {
                    Content = "LEGACY - " + DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)
                });
        }

        private class LegacyMessageSerializer : IMessageSerializer
        {
            private readonly JsonSerializerSettings _settings = new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None
            };

            [SuppressMessage("", "SA1011", Justification = "False positive")]
            [SuppressMessage("ReSharper", "ReturnTypeCanBeNotNullable", Justification = "Interface contract")]
            public byte[]? Serialize(
                object? message,
                MessageHeaderCollection messageHeaders,
                MessageSerializationContext context) =>
                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message, _settings));

            [SuppressMessage("", "SA1011", Justification = "False positive")]
            public (object?, Type) Deserialize(
                byte[]? message,
                MessageHeaderCollection messageHeaders,
                MessageSerializationContext context)
            {
                var deserialized = message != null
                    ? JsonConvert.DeserializeObject<LegacyMessage>(Encoding.ASCII.GetString(message))
                    : null;
                return (deserialized, typeof(LegacyMessage));
            }

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
                Task.FromResult(Deserialize(message, messageHeaders, context));
        }
    }
}
