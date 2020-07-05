// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Main.UseCases.Producing.Rabbit.Basic
{
    public class SimpleQueuePublishUseCase : UseCase
    {
        public SimpleQueuePublishUseCase()
        {
            Title = "Simple publish to a queue";
            Description = "The simplest way to publish a message to a queue (without using an exchange).";
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSilverback()
            .UseModel()
            .WithConnectionToMessageBroker(options => options.AddRabbit());

        protected override void Configure(IBusConfigurator configurator, IServiceProvider serviceProvider) =>
            configurator.Connect(
                endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(
                        new RabbitQueueProducerEndpoint("silverback-examples-events-queue")
                        {
                            Queue = new RabbitQueueConfig
                            {
                                IsDurable = true,
                                IsExclusive = false,
                                IsAutoDeleteEnabled = false
                            },
                            Connection = new RabbitConnectionConfig
                            {
                                HostName = "localhost",
                                UserName = "guest",
                                Password = "guest"
                            }
                        }));

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisher = serviceProvider.GetService<IEventPublisher>();

            await publisher.PublishAsync(
                new SimpleIntegrationEvent
                {
                    Content = DateTime.Now.ToString("HH:mm:ss.fff", provider: CultureInfo.InvariantCulture)
                });
        }
    }
}
