// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common;
using Silverback.Examples.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    public class EncryptionAvroUseCase : UseCase
    {
        public EncryptionAvroUseCase()
        {
            Title = "Encryption (Avro)";
            Description = "End-to-end encrypt the message content. This sample uses Aes with a 256 bit key.";
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSilverback()
            .UseModel()
            .WithConnectionToMessageBroker(options => options.AddKafka());

        protected override void Configure(BusConfigurator configurator, IServiceProvider serviceProvider)
        {
            configurator.Connect(endpoints => endpoints
                .AddOutbound<AvroMessage>(new KafkaProducerEndpoint("silverback-examples-avro-encrypted")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092"
                    },
                    Encryption = new SymmetricEncryptionSettings
                    {
                        Key = Constants.AesEncryptionKey
                    },
                    Serializer = new AvroMessageSerializer<AvroMessage>
                    {
                        SchemaRegistryConfig = new SchemaRegistryConfig
                        {
                            Url = "localhost:8081"
                        },
                        AvroSerializerConfig = new AvroSerializerConfig
                        {
                            AutoRegisterSchemas = true
                        }
                    }
                }));
        }

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisher = serviceProvider.GetService<IPublisher>();

            await publisher.PublishAsync(new AvroMessage
            {
                key = Guid.NewGuid().ToString("N"),
                content = DateTime.Now.ToString("HH:mm:ss.fff")
            });
        }
    }
}