// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Basic
{
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Invoked by test framework")]
    [SuppressMessage("", "CA1822", Justification = "Startup contract")]
    public class AvroSerializerUseCaseStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddKafka())
                .AddEndpoints(
                    endpoints => endpoints
                        .AddOutbound<AvroMessage>(
                            new KafkaProducerEndpoint("silverback-examples-avro")
                            {
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
                                },
                                Configuration = new KafkaProducerConfig
                                {
                                    BootstrapServers = "PLAINTEXT://localhost:9092"
                                }
                            }));
        }

        public void Configure()
        {
        }
    }
}
