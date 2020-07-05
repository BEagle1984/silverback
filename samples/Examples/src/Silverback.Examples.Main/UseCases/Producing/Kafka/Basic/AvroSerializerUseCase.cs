// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Threading.Tasks;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Basic
{
    public class AvroSerializerUseCase : UseCase
    {
        public AvroSerializerUseCase()
        {
            Title = "Avro serializer";
            Description =
                "Serialize the messages in Apache Avro format using a schema registry.";
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSilverback()
            .UseModel()
            .WithConnectionToMessageBroker(options => options.AddKafka());

        protected override void Configure(IBusConfigurator configurator, IServiceProvider serviceProvider) =>
            configurator.Connect(
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

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisher = serviceProvider.GetService<IPublisher>();

            await publisher.PublishAsync(
                new AvroMessage
                {
                    key = Guid.NewGuid().ToString("N"),
                    content = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)
                });
        }
    }
}
