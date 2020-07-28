﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Basic
{
    public class TranslateUseCase : UseCase, ISubscriber
    {
        public TranslateUseCase()
        {
            Title = "Message translation";
            Description = "A translation/mapping method is used to transform the messages to be published.";
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Subscriber")]
        [SuppressMessage("ReSharper", "CA1822", Justification = "Subscriber cannot be static")]
        public IMessage OnSimpleEvent(SimpleEvent message) => new SimpleIntegrationEvent { Content = message.Content };

        protected override void ConfigureServices(IServiceCollection services) => services
            .AddSilverback()
            .UseModel()
            .WithConnectionToMessageBroker(options => options.AddKafka())
            .AddScopedSubscriber<TranslateUseCase>();

        protected override void Configure(IBusConfigurator configurator, IServiceProvider serviceProvider) =>
            configurator.Connect(
                endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(
                        new KafkaProducerEndpoint("silverback-examples-events")
                        {
                            Configuration = new KafkaProducerConfig
                            {
                                BootstrapServers = "PLAINTEXT://localhost:9092"
                            }
                        }));

        protected override async Task Execute(IServiceProvider serviceProvider)
        {
            var publisher = serviceProvider.GetService<IEventPublisher>();

            await publisher.PublishAsync(
                new SimpleEvent
                {
                    Content = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)
                });
        }
    }
}