// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Basic
{
    public class NewtonsoftSerializerUseCase : UseCase
    {
        public NewtonsoftSerializerUseCase()
        {
            Title = "Newtonsoft serializer";
            Description =
                "Serialize and deserialize the messages using the legacy Newtonsoft.Json based serializer.";
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSilverback()
            .UseModel()
            .WithConnectionToMessageBroker(options => options.AddKafka());

        protected override void Configure(IBusConfigurator configurator, IServiceProvider serviceProvider) =>
            configurator.Connect(
                endpoints => endpoints
                    .AddOutbound<SimpleIntegrationEvent>(
                        new KafkaProducerEndpoint("silverback-examples-newtonsoft")
                        {
                            Serializer = new NewtonsoftJsonMessageSerializer(),
                            Configuration = new KafkaProducerConfig
                            {
                                BootstrapServers = "PLAINTEXT://localhost:9092"
                            }
                        }));

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisher = serviceProvider.GetService<IEventPublisher>();

            await publisher.PublishAsync(
                new SimpleIntegrationEvent
                    { Content = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) });
        }
    }
}
