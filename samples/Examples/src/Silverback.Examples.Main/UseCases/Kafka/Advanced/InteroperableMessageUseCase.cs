// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Examples.Main.UseCases.Kafka.Advanced
{
    public class InteroperableMessageUseCase : UseCase
    {
        public InteroperableMessageUseCase()
        {
            Title = "Interoperability";
            Description = "A message is sent using a custom serializer and without the headers needed by Silverback" +
                          "to deserialize it. The consumer serializer is tweaked to work with this 'legacy' messages.";
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSilverback()
            .UseModel()
            .WithConnectionToKafka();

        protected override void Configure(BusConfigurator configurator, IServiceProvider serviceProvider) { }

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var broker = serviceProvider.GetRequiredService<IBroker>();
            await broker.GetProducer(CreateEndpoint("silverback-examples-legacy-messages"))
                .ProduceAsync(new LegacyMessage { Content = "LEGACY - " + DateTime.Now.ToString("HH:mm:ss.fff") });
        }

        private KafkaProducerEndpoint CreateEndpoint(string name) =>
            new KafkaProducerEndpoint(name)
            {
                Serializer = new LegacyMessageSerializer(),
                Configuration = new KafkaProducerConfig
                {
                    BootstrapServers = "PLAINTEXT://localhost:9092"
                }
            };
        
        public class LegacyMessageSerializer : IMessageSerializer
        {
            private readonly JsonSerializerSettings _settings = new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None
            };

            public byte[] Serialize(object message, MessageHeaderCollection messageHeaders) =>
                Encoding.ASCII.GetBytes(
                    JsonConvert.SerializeObject(message, _settings));

            public object Deserialize(byte[] message, MessageHeaderCollection messageHeaders) =>
                JsonConvert.DeserializeObject<LegacyMessage>(
                    Encoding.ASCII.GetString(message));
        }
    }
}