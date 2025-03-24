// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Publishing;
using Silverback.TestBench.ViewModel;
using Silverback.TestBench.ViewModel.Logs;
using Silverback.TestBench.ViewModel.Topics;

namespace Silverback.TestBench.Producer;

public class ProducerBackgroundService : BackgroundService
{
    private readonly IPublisher _publisher;

    private readonly MessagesTracker _messagesTracker;

    private readonly MainViewModel _mainViewModel;

    private readonly ILogger<ProducerBackgroundService> _logger;

    public ProducerBackgroundService(
        IPublisher publisher,
        MessagesTracker messagesTracker,
        MainViewModel mainViewModel,
        ILogger<ProducerBackgroundService> logger)
    {
        _publisher = publisher;
        _messagesTracker = messagesTracker;
        _mainViewModel = mainViewModel;
        _logger = logger;
    }

    [SuppressMessage("Usage", "VSTHRD110:Observe result of async calls", Justification = "Fire and forget")]
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (TopicViewModel topic in _mainViewModel.Topics)
        {
            Task.Run(() => ProduceAsync(topic, stoppingToken), stoppingToken);
        }

        return Task.CompletedTask;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Log and continue in any case")]
    private async Task ProduceAsync(TopicViewModel topic, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_mainViewModel.IsProducing || !topic.IsEnabled)
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
                _mainViewModel.Trace.TraceProduced(
                    message.MessageId,
                    message.TargetTopicViewModel,
                    new LogEntry(DateTime.UtcNow, "Message produced", null));

                TimeSpan produceDelay = topic.ProduceDelay;
                double speedMultiplier = _mainViewModel.ProduceSpeedMultiplier;

                if (produceDelay != TimeSpan.Zero && speedMultiplier > 0)
                {
                    TimeSpan produceSpeedMultiplier = produceDelay / speedMultiplier;
                    await Task.Delay(produceSpeedMultiplier, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Produce of message {MessageId} failed", message.MessageId);
                _mainViewModel.Logs.AddError(DateTime.UtcNow, $"Produce of message {message.MessageId} failed: {ex}", null);
                _messagesTracker.TrackProduceError(message);
            }
        }
    }
}
