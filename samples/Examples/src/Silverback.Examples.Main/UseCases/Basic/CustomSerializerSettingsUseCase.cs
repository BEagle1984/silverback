// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;

namespace Silverback.Examples.Main.UseCases.Basic
{
    public class CustomSerializerSettingsUseCase : UseCase
    {
        public CustomSerializerSettingsUseCase() : base("Custom serializer settings", 30)
        {
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddBus(options => options.UseModel())
            .AddBroker<KafkaBroker>();

        protected override void Configure(BusConfigurator configurator, IServiceProvider serviceProvider) =>
            configurator.Connect(endpoints =>
                endpoints
                    .AddOutbound<IIntegrationEvent>(CreateEndpoint("silverback-examples-custom-serializer-events")));

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisher = serviceProvider.GetService<IEventPublisher>();

            await publisher.PublishAsync(new CustomSerializedIntegrationEvent { Content = DateTime.Now.ToString("HH:mm:ss.fff") });
        }

        private IEndpoint CreateEndpoint(string name) =>
            new KafkaProducerEndpoint(name)
            {
                Serializer = GetSerializer(),
                Configuration = new KafkaProducerConfig
                {
                    BootstrapServers = "PLAINTEXT://kafka:9092",
                    ClientId = GetType().FullName
                }
            };

        private static JsonMessageSerializer GetSerializer()
        {
            var serializer = new JsonMessageSerializer
            {
                Encoding = MessageEncoding.Unicode
            };

            serializer.Settings.Formatting = Newtonsoft.Json.Formatting.Indented;

            return serializer;
        }
    }
}