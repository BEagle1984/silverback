// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.TestBench.ViewModel;
using Silverback.TestBench.ViewModel.Logs;
using Silverback.TestBench.ViewModel.Topics;

namespace Silverback.TestBench.Producer;

public sealed class MessagesTracker : IDisposable, IAsyncDisposable
{
    private static readonly TimeSpan LostMessagesThreshold = TimeSpan.FromMinutes(5);

    private readonly MainViewModel _mainViewModel;

    private readonly ILogger<MessagesTracker> _logger;

    private readonly ConcurrentDictionary<string, RoutableTestBenchMessage> _pendingMessages = new();

    private readonly Timer _checkLostMessagesTimer;

    public MessagesTracker(MainViewModel mainViewModel, ILogger<MessagesTracker> logger)
    {
        _mainViewModel = mainViewModel;
        _logger = logger;
        _checkLostMessagesTimer = new Timer(_ => CheckLostMessages(), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
    }

    public void TrackProduced(RoutableTestBenchMessage message)
    {
        _pendingMessages.TryAdd(message.MessageId, message);

        _mainViewModel.OverallMessagesStatistics.IncrementProducedCount();
        message.TargetTopicViewModel.Statistics.IncrementProducedCount();
    }

    public void TrackProduceError(RoutableTestBenchMessage message)
    {
        _mainViewModel.OverallMessagesStatistics.IncrementProduceErrorsCount();
        message.TargetTopicViewModel.Statistics.IncrementProduceErrorsCount();
    }

    public TopicViewModel? TrackConsumed(string subscribedTopicName)
    {
        _mainViewModel.OverallMessagesStatistics.IncrementConsumedCount();

        if (_mainViewModel.TryGetTopic(subscribedTopicName, out TopicViewModel? topicViewModel))
            topicViewModel.Statistics.IncrementConsumedCount();

        return topicViewModel;
    }

    public TopicViewModel? TrackProcessed(string subscribedTopicName, string messageId)
    {
        if (!_pendingMessages.TryRemove(messageId, out _))
            return null;

        _mainViewModel.OverallMessagesStatistics.IncrementProcessedCount();

        if (_mainViewModel.TryGetTopic(subscribedTopicName, out TopicViewModel? topicViewModel))
            topicViewModel.Statistics.IncrementProcessedCount();

        return topicViewModel;
    }

    public bool TrackLost(TopicViewModel topicViewModel, string messageId)
    {
        if (!_pendingMessages.TryRemove(messageId, out _))
            return false;

        _mainViewModel.OverallMessagesStatistics.IncrementLostCount();
        topicViewModel.Statistics.IncrementLostCount();

        return true;
    }

    public void Dispose() => _checkLostMessagesTimer.Dispose();

    public async ValueTask DisposeAsync() => await _checkLostMessagesTimer.DisposeAsync();

    private void CheckLostMessages()
    {
        DateTime threshold = DateTime.UtcNow - LostMessagesThreshold;

        List<RoutableTestBenchMessage> lostMessages =
            _pendingMessages.Where(pair => pair.Value.CreatedAt <= threshold).Select(pair => pair.Value).ToList();

        foreach (RoutableTestBenchMessage message in lostMessages)
        {
            if (TrackLost(message.TargetTopicViewModel, message.MessageId))
            {
                _logger.LogCritical(
                    "Message {MessageId} produced on topic {Topic} was not consumed within the expected time ({Threshold})" +
                    "and is considered lost",
                    message.MessageId,
                    message.TargetTopicViewModel.TopicName,
                    LostMessagesThreshold);

                LogEntry logEntry = _mainViewModel.Logs.AddFatal(
                    DateTime.UtcNow,
                    $"Message {message.MessageId} produced on topic {message.TargetTopicViewModel.TopicName} was not consumed within the expected time ({LostMessagesThreshold}) and is considered lost",
                    null);

                _mainViewModel.Trace.TraceLost(message.MessageId, message.TargetTopicViewModel, logEntry);
            }
        }
    }
}
