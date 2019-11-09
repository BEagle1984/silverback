// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common.Messages;
using Silverback.Examples.Common.Serialization;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

namespace Silverback.Examples.Main.UseCases.Advanced
{
    public class InteroperableMessageUseCase : UseCase
    {
        public InteroperableMessageUseCase() : base("Interoperable incoming message (free schema, not published by Silverback)", 10)
        {
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

        private IEndpoint CreateEndpoint(string name) =>
            new KafkaProducerEndpoint(name)
            {
                Serializer = new LegacyMessageSerializer(),
                Configuration = new KafkaProducerConfig
                {
                    BootstrapServers = "PLAINTEXT://localhost:9092",
                    ClientId = GetType().FullName
                }
            };
    }
}