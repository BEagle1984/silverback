// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.TestBench.Configuration.Models;
using Silverback.TestBench.Producer.Models;

namespace Silverback.TestBench.Producer;

public sealed class MessagesTracker : IDisposable, IAsyncDisposable
{
    private static readonly TimeSpan LostMessagesThreshold = TimeSpan.FromMinutes(1);

    private readonly ILogger<MessagesTracker> _logger;

    private readonly ConcurrentDictionary<string, RoutableTestBenchMessage> _pendingMessages = new();

    private readonly Timer _checkLostMessagesTimer;

    public MessagesTracker(ILogger<MessagesTracker> logger)
    {
        _logger = logger;
        _checkLostMessagesTimer = new Timer(_ => CheckLostMessages(), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
    }

    public MessagesStats GlobalStats { get; } = new(null);

    public Dictionary<string, MessagesStats> StatsByTopic { get; } = [];

    public void InitializeTopic(TopicConfiguration topicConfiguration) => StatsByTopic.Add(topicConfiguration.TopicName, new MessagesStats(topicConfiguration));

    public void TrackProduced(RoutableTestBenchMessage message)
    {
        _pendingMessages.TryAdd(message.MessageId, message);

        lock (GlobalStats)
        {
            GlobalStats.ProducedCount++;
        }

        lock (StatsByTopic)
        {
            StatsByTopic[message.TargetTopicConfiguration.TopicName].ProducedCount++;
        }
    }

    public void TrackFailedProduce(RoutableTestBenchMessage message)
    {
        lock (GlobalStats)
        {
            GlobalStats.FailedProduceCount++;
        }

        lock (StatsByTopic)
        {
            StatsByTopic[message.TargetTopicConfiguration.TopicName].FailedProduceCount++;
        }
    }

    public void TrackConsumed(string subscribedTopicName)
    {
        lock (GlobalStats)
        {
            GlobalStats.ConsumedCount++;
        }

        lock (StatsByTopic)
        {
            if (StatsByTopic.TryGetValue(subscribedTopicName, out MessagesStats? stats))
                stats.ConsumedCount++;
        }
    }

    public void TrackProcessed(string subscribedTopicName, string messageId)
    {
        if (!_pendingMessages.TryRemove(messageId, out _))
            return;

        lock (GlobalStats)
        {
            GlobalStats.ProcessedCount++;
        }

        lock (StatsByTopic)
        {
            if (StatsByTopic.TryGetValue(subscribedTopicName, out MessagesStats? stats))
                stats.ProcessedCount++;
        }
    }

    public bool TrackLost(TopicConfiguration topicConfiguration, string messageId)
    {
        if (!_pendingMessages.TryRemove(messageId, out _))
            return false;

        lock (GlobalStats)
        {
            GlobalStats.LostCount++;
        }

        lock (StatsByTopic)
        {
            if (StatsByTopic.TryGetValue(topicConfiguration.TopicName, out MessagesStats? stats))
                stats.LostCount++;
        }

        return true;
    }

    public void Dispose() => _checkLostMessagesTimer.Dispose();

    public async ValueTask DisposeAsync() => await _checkLostMessagesTimer.DisposeAsync();

    private void CheckLostMessages()
    {
        DateTime threshold = DateTime.Now - LostMessagesThreshold;

        List<RoutableTestBenchMessage> lostMessages =
            _pendingMessages.Where(pair => pair.Value.CreatedAt <= threshold).Select(pair => pair.Value).ToList();

        foreach (RoutableTestBenchMessage message in lostMessages)
        {
            if (TrackLost(message.TargetTopicConfiguration, message.MessageId))
            {
                _logger.LogCritical(
                    "Message {MessageId} produced on topic {Topic} was not consumed within the expected time ({Threshold})" +
                    "and is considered lost",
                    message.MessageId,
                    message.TargetTopicConfiguration.TopicName,
                    LostMessagesThreshold);
            }
        }
    }
}
