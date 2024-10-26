// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Publishing;
using Silverback.TestBench.Configuration;
using Silverback.TestBench.Configuration.Models;

namespace Silverback.TestBench.Producer;

public class ProducerBackgroundService : BackgroundService
{
    private readonly IPublisher _publisher;

    private readonly MessagesTracker _messagesTracker;

    private readonly ILogger<ProducerBackgroundService> _logger;

    public ProducerBackgroundService(
        IPublisher publisher,
        MessagesTracker messagesTracker,
        ILogger<ProducerBackgroundService> logger)
    {
        _publisher = publisher;
        _messagesTracker = messagesTracker;
        _logger = logger;
    }

    public bool IsEnabled { get; private set; }

    public void Enable() => IsEnabled = true;

    public void Disable() => IsEnabled = false;

    [SuppressMessage("Usage", "VSTHRD110:Observe result of async calls", Justification = "Fire and forget")]
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (TopicConfiguration topic in TopicsConfiguration.All)
        {
            _messagesTracker.InitializeTopic(topic);

            if (topic.Enabled)
                Task.Run(() => ProduceAsync(topic, stoppingToken), stoppingToken);
        }

        return Task.CompletedTask;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Log and continue in any case")]
    private async Task ProduceAsync(TopicConfiguration topic, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!IsEnabled)
            {
                await Task.Delay(1000, stoppingToken);
                continue;
            }

            using Activity activity = new("Produce");
            activity.Start();

            RoutableTestBenchMessage message = new(topic);
            try
            {
                await _publisher.PublishAsync(message, stoppingToken);
                _messagesTracker.TrackProduced(message);

                if (topic.ProduceDelay != default)
                    await Task.Delay(topic.ProduceDelay, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Produce of message {MessageId} failed", message.MessageId);
                _messagesTracker.TrackFailedProduce(message);
            }
        }
    }
}
