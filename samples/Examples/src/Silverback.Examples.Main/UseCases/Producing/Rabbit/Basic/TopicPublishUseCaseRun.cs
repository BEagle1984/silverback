// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Examples.Common.Messages;
using Silverback.Examples.Main.Menu;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Main.UseCases.Producing.Rabbit.Basic
{
    [SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Invoked by test framework")]
    public class TopicPublishUseCaseRun : IAsyncRunnable
    {
        private readonly IEventPublisher _publisher;

        private readonly ILogger<TopicPublishUseCaseRun> _logger;

        public TopicPublishUseCaseRun(IEventPublisher publisher, ILogger<TopicPublishUseCaseRun> logger)
        {
            _publisher = publisher;
            _logger = logger;
        }

        public async Task Run()
        {
            await _publisher.PublishAsync(
                new RoutedSimpleIntegrationEvent
                {
                    Key = "interesting.order.event",
                    Content = $"This is an interesting.order.event -> {DateTime.Now:HH:mm:ss.fff}"
                });
            await _publisher.PublishAsync(
                new RoutedSimpleIntegrationEvent
                {
                    Key = "interesting.basket.event",
                    Content = $"This is an interesting.basket.event -> {DateTime.Now:HH:mm:ss.fff}"
                });
            await _publisher.PublishAsync(
                new RoutedSimpleIntegrationEvent
                {
                    Key = "useless.basket.event",
                    Content = $"This is a useless.order.event -> {DateTime.Now:HH:mm:ss.fff}"
                });

            _logger.LogInformation(
                "Published 3 events with routing keys: 'interesting.order.event', 'interesting.basket.event'" +
                " and 'useless.basket.event'. (The consumer should consume only the first two, " +
                "since the bounding key used is 'interesting.*.event'.)");
        }
    }
}
