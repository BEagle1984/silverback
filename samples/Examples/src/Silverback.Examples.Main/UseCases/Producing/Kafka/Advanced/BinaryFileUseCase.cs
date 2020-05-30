// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    public class BinaryFileUseCase : UseCase
    {
        public BinaryFileUseCase()
        {
            Title = "Publish raw binary file";
            Description = "Use a BinaryFileMessage to publish a raw binary.";
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSilverback()
            .UseModel()
            .WithConnectionToMessageBroker(options => options.AddKafka());

        protected override void Configure(IBusConfigurator configurator, IServiceProvider serviceProvider) =>
            configurator.Connect(endpoints => endpoints
                .AddOutbound<IBinaryFileMessage>(new KafkaProducerEndpoint("silverback-examples-binaries")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://localhost:9092"
                    }
                }));

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisher = serviceProvider.GetService<IPublisher>();

            await publisher.PublishAsync(new BinaryFileMessage
            {
                Content = new byte[] { 0xBE, 0xA6, 0x13, 0x19, 0x84 },
                ContentType = "application/awesome"
            });
        }
    }
}