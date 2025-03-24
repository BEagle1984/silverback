// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Confluent.Kafka;
using Ductus.FluentDocker.Services;
using Microsoft.Extensions.Logging;
using Silverback.TestBench.Producer;
using Silverback.TestBench.Utils;
using Silverback.TestBench.ViewModel;
using Silverback.TestBench.ViewModel.Containers;
using Silverback.TestBench.ViewModel.Logs;
using Silverback.TestBench.ViewModel.Topics;

namespace Silverback.TestBench.Containers;

public sealed partial class ContainerLogParser : IDisposable
{
    private readonly IContainerService _containerService;

    private readonly ContainerInstanceViewModel _container;

    private readonly MainViewModel _mainViewModel;

    private readonly MessagesTracker _messagesTracker;

    private readonly ILogger<ContainerLogParser> _logger;

    private readonly CancellationTokenSource _stoppingTokenSource = new();

    [SuppressMessage("Usage", "VSTHRD110:Observe result of async calls", Justification = "Fire and forget")]
    public ContainerLogParser(
        ContainerInstanceViewModel containerInstance,
        MainViewModel mainViewModel,
        MessagesTracker messagesTracker,
        ILogger<ContainerLogParser> logger)
    {
        _containerService = containerInstance.ContainerService;
        _container = containerInstance;
        _mainViewModel = mainViewModel;
        _messagesTracker = messagesTracker;
        _logger = logger;

        Task.Run(() => TailLogAsync(_stoppingTokenSource.Token));
    }

    public void Dispose()
    {
        _stoppingTokenSource.Cancel();
        _stoppingTokenSource.Dispose();
    }

    [GeneratedRegex(@"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}) \[(?<logLevel>[A-Z]+)\] (?<message>.+)$")]
    private static partial Regex LogLineRegex();

    [GeneratedRegex(@"Application started\.")]
    private static partial Regex ApplicationStartedRegex();

    [GeneratedRegex(@"Application is shutting down\.\.\.")]
    private static partial Regex ApplicationShuttingDownRegex();

    [GeneratedRegex(@"Processing consumed message.*endpointName: (?<topicName>.*?)(?=,.*?messageId: (?<messageId>.+?)(?=,|$))")]
    private static partial Regex MessageProcessingRegex();

    [GeneratedRegex(@"Successfully processed message '(?<messageId>[^']+)' from topic '(?<topicName>[^']+)'")]
    private static partial Regex MessageProcessedRegex();

    [GeneratedRegex(@"Assigned partition (?<topicName>.+)\[(?<partition>\d+)\]")]
    private static partial Regex PartitionAssignedRegex();

    [GeneratedRegex(@"Revoked partition (?<topicName>.+)\[(?<partition>\d+)\]")]
    private static partial Regex PartitionRevokedRegex();

    [GeneratedRegex(@"Consumer subscribed to (?<topicName>.+?)\.")]
    private static partial Regex TopicSubscribedRegex();

    [GeneratedRegex(@"All clients disconnected\.")]
    private static partial Regex AllClientsDisconnectedRegex();

    [GeneratedRegex(@"^\$share/[^/]+/")]
    private static partial Regex SharedSubscriptionPrefixRegex();

    private static async Task WaitFileExistsAsync(string logPath)
    {
        while (!File.Exists(logPath))
        {
            await Task.Delay(500);
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Logged")]
    private async Task TailLogAsync(CancellationToken stoppingToken)
    {
        string logPath = Path.Combine(FileSystemHelper.LogsFolder, $"{_containerService.Name}.log");

        try
        {
            await WaitFileExistsAsync(logPath);
            await TailLogAsync(logPath, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error while tailing the log file {LogPath}", logPath);
        }
    }

    private async Task TailLogAsync(string logPath, CancellationToken stoppingToken)
    {
        await using FileStream fileStream = new(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using StreamReader reader = new(fileStream);

        while (!stoppingToken.IsCancellationRequested)
        {
            while (!reader.EndOfStream && !stoppingToken.IsCancellationRequested)
            {
                ParseLogLine(await reader.ReadLineAsync(stoppingToken));
            }

            await Task.Delay(500, stoppingToken);
        }
    }

    private void ParseLogLine(string? logLine)
    {
        if (logLine == null)
            return;

        Match match = LogLineRegex().Match(logLine);

        if (!match.Success)
            return;

        string logLevel = match.Groups["logLevel"].Value;
        string message = match.Groups["message"].Value;
        DateTime timestamp = DateTime.ParseExact(match.Groups["timestamp"].Value, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);

        switch (logLevel)
        {
            case "WRN":
                _container.Statistics.IncrementWarningsCount();
                _mainViewModel.Logs.AddWarning(timestamp, message, _container);
                break;
            case "ERR":
                _container.Statistics.IncrementErrorsCount();
                _mainViewModel.Logs.AddError(timestamp, message, _container);
                break;
            case "FTL":
                _container.Statistics.IncrementFatalErrorsCount();
                _mainViewModel.Logs.AddFatal(timestamp, message, _container);
                break;
            case "INF":
                ParseInfoMessage(message, timestamp);
                break;
            case "VRB":
                ParseVerboseMessage(message, timestamp);
                break;
        }
    }

    private void ParseInfoMessage(string message, DateTime timestamp)
    {
        if (MatchProcessing(message, timestamp))
            return;

        if (MatchProcessed(message, timestamp))
            return;

        if (MatchStarted(message, timestamp))
            return;

        if (MatchPartitionAssigned(message, timestamp))
            return;

        if (MatchPartitionRevoked(message, timestamp))
            return;

        if (MatchTopicSubscribed(message, timestamp))
            return;

        MatchStopped(message, timestamp);
    }

    private void ParseVerboseMessage(string message, DateTime timestamp) =>
        MatchAllClientsDisconnected(message, timestamp);

    private bool MatchProcessing(string message, DateTime timestamp)
    {
        Match match = MessageProcessingRegex().Match(message);

        if (!match.Success)
            return false;

        _container.Statistics.IncrementConsumedMessagesCount();
        TopicViewModel? topicViewModel = _messagesTracker.TrackConsumed(match.Groups["topicName"].Value);

        if (topicViewModel != null)
        {
            _mainViewModel.Trace.TraceProcessing(
                match.Groups["messageId"].Value,
                topicViewModel,
                new LogEntry(timestamp, message, _container));
        }

        return true;
    }

    private bool MatchProcessed(string message, DateTime timestamp)
    {
        Match match = MessageProcessedRegex().Match(message);
        if (!match.Success)
            return false;

        _container.Statistics.IncrementProcessedMessagesCount();
        TopicViewModel? topicViewModel = _messagesTracker.TrackProcessed(match.Groups["topicName"].Value, match.Groups["messageId"].Value);

        if (topicViewModel != null)
        {
            _mainViewModel.Trace.TraceProcessed(
                match.Groups["messageId"].Value,
                topicViewModel,
                new LogEntry(timestamp, message, _container));
        }

        return true;
    }

    private bool MatchStarted(string message, DateTime timestamp)
    {
        if (_container.Started.HasValue)
            return false;

        Match match = ApplicationStartedRegex().Match(message);

        if (!match.Success)
            return false;

        _container.SetStarted(timestamp);
        return true;
    }

    [SuppressMessage("Usage", "VSTHRD110:Observe result of async calls", Justification = "Fire and forget")]
    private void MatchStopped(string message, DateTime timestamp)
    {
        if (_container.Stopped.HasValue)
            return;

        Match match = ApplicationShuttingDownRegex().Match(message);

        if (!match.Success)
            return;

        _container.SetStopped(timestamp);

        _mainViewModel.Logs.AddInformation(timestamp, "Shutdown initiated", _container);

        // Delayed dispose to allow the log parser to finish processing the last messages
        Task.Run(
            async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(5), _stoppingTokenSource.Token);
                Dispose();
            });
    }

    private bool MatchPartitionAssigned(string message, DateTime timestamp)
    {
        Match match = PartitionAssignedRegex().Match(message);

        if (!match.Success)
            return false;

        TopicPartition topicPartition = new(match.Groups["topicName"].Value, int.Parse(match.Groups["partition"].Value, CultureInfo.InvariantCulture));
        Application.Current.Dispatcher.Invoke(() => _container.AssignedKafkaPartitions.Add(topicPartition));

        if (_mainViewModel.TryGetTopic(topicPartition.Topic, out TopicViewModel? topicViewModel) &&
            topicViewModel is KafkaTopicViewModel kafkaTopicViewModel)
        {
            kafkaTopicViewModel.TrackPartitionAssigned(timestamp, topicPartition.Partition, _container);
        }

        _mainViewModel.Logs.AddInformation(timestamp, message, _container);

        return true;
    }

    private bool MatchPartitionRevoked(string message, DateTime timestamp)
    {
        Match match = PartitionRevokedRegex().Match(message);

        if (!match.Success)
            return false;

        TopicPartition topicPartition = new(match.Groups["topicName"].Value, int.Parse(match.Groups["partition"].Value, CultureInfo.InvariantCulture));
        Application.Current.Dispatcher.Invoke(() => _container.AssignedKafkaPartitions.Remove(topicPartition));

        if (_mainViewModel.TryGetTopic(topicPartition.Topic, out TopicViewModel? topicViewModel) &&
            topicViewModel is KafkaTopicViewModel kafkaTopicViewModel)
        {
            kafkaTopicViewModel.TrackPartitionRevoked(timestamp, topicPartition.Partition, _container);
        }

        _mainViewModel.Logs.AddInformation(timestamp, message, _container);

        return true;
    }

    private bool MatchTopicSubscribed(string message, DateTime timestamp)
    {
        Match match = TopicSubscribedRegex().Match(message);

        if (!match.Success)
            return false;

        string topicName = match.Groups["topicName"].Value;
        Application.Current.Dispatcher.Invoke(() => _container.SubscribedMqttTopics.Add(topicName));

        topicName = SharedSubscriptionPrefixRegex().Replace(topicName, string.Empty);

        if (_mainViewModel.TryGetTopic(topicName, out TopicViewModel? topicViewModel) &&
            topicViewModel is MqttTopicViewModel mqttTopicViewModel)
        {
            mqttTopicViewModel.TrackSubscribed(timestamp, _container);
        }

        _mainViewModel.Logs.AddInformation(timestamp, message, _container);

        return true;
    }

    private void MatchAllClientsDisconnected(string message, DateTime timestamp)
    {
        Match match = AllClientsDisconnectedRegex().Match(message);

        if (!match.Success)
            return;

        _mainViewModel.Logs.AddInformation(timestamp, message, _container);

        foreach (string topicName in _container.SubscribedMqttTopics)
        {
            if (_mainViewModel.TryGetTopic(topicName, out TopicViewModel? topicViewModel) &&
                topicViewModel is MqttTopicViewModel mqttTopicViewModel)
            {
                mqttTopicViewModel.TrackUnsubscribed(timestamp, _container);
            }
        }
    }
}
