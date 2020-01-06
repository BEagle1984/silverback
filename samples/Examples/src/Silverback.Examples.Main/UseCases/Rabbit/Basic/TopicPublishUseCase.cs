// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Main.UseCases.Rabbit.Basic
{
    public class TopicPublishUseCase : UseCase
    {
        public TopicPublishUseCase()
        {
            Title = "Publish to a topic exchange";
            Description = "Publish some messages to a topic exchange (with different routing keys). " +
                          "The topic exchange routes according to the routing key / binding key. " +
                          "(Only the messages with routing key starting with \"interesting.*.event\" will be " +
                          "consumed.)";
        }

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSilverback()
            .UseModel()
            .WithConnectionToRabbit();

        protected override void Configure(BusConfigurator configurator, IServiceProvider serviceProvider) =>
            configurator.Connect(endpoints => endpoints
                .AddOutbound<IIntegrationEvent>(new RabbitExchangeProducerEndpoint("silverback-examples-events-topic")
                {
                    Exchange = new RabbitExchangeConfig
                    {
                        IsDurable = true,
                        IsAutoDeleteEnabled = false,
                        ExchangeType = ExchangeType.Topic
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

            await publisher.PublishAsync(new RoutedSimpleIntegrationEvent
            {
                Key = "interesting.order.event",
                Content = $"This is an interesting.order.event -> {DateTime.Now:HH:mm:ss.fff}"
            });
            await publisher.PublishAsync(new RoutedSimpleIntegrationEvent
            {
                Key = "interesting.basket.event",
                Content = $"This is an interesting.basket.event -> {DateTime.Now:HH:mm:ss.fff}"
            });
            await publisher.PublishAsync(new RoutedSimpleIntegrationEvent
            {
                Key = "useless.basket.event",
                Content = $"This is a useless.order.event -> {DateTime.Now:HH:mm:ss.fff}"
            });
            
            serviceProvider.GetRequiredService<ILogger<TopicPublishUseCase>>().LogInformation(
                "Published 3 events with routing keys: 'interesting.order.event', 'interesting.basket.event'" +
                " and 'useless.basket.event'. (The consumer should consume only the first two, " +
                "since the bounding key used is 'interesting.*.event'.)");
        }
    }
}