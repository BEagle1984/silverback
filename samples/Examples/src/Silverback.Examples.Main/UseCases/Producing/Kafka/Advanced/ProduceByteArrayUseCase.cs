// Copyright (c) 2020 Sergio Aquilini
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

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    public class ProduceByteArrayUseCase : UseCase
    {
        public ProduceByteArrayUseCase()
        {
            Title = "Produce a raw byte array";
            Description = "Publish a pre-serialized message to a topic.";
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSilverback()
            .UseModel()
            .WithConnectionToMessageBroker(options => options.AddKafka());

        protected override void Configure(IBusConfigurator configurator, IServiceProvider serviceProvider) =>
            configurator.Connect();

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var byteArray = Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(
                    new SimpleIntegrationEvent
                    {
                        Content = DateTime.Now.ToString("HH:mm:ss.fff")
                    },
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All
                    }));

            var producer = serviceProvider.GetService<IBroker>().GetProducer(
                new KafkaProducerEndpoint("silverback-examples-events")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092"
                    }
                });

            await producer.ProduceAsync(
                byteArray,
                new MessageHeader[]
                {
                    new MessageHeader("use-case", "pre-serialized"),
                });
        }
    }
}