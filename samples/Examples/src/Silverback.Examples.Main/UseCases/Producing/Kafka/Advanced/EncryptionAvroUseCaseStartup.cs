// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common;
using Silverback.Examples.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Serialization;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Invoked by test framework")]
    [SuppressMessage("", "CA1822", Justification = "Startup contract")]
    public class EncryptionAvroUseCaseStartup
    {
        public void ConfigureServices(IServiceCollection services) => services
            .AddSilverback()
            .UseModel()
            .WithConnectionToMessageBroker(options => options.AddKafka())
            .AddEndpoints(
                endpoints => endpoints
                    .AddOutbound<AvroMessage>(
                        new KafkaProducerEndpoint("silverback-examples-avro-encrypted")
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

        public void Configure()
        {
        }
    }
}
