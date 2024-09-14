// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Ductus.FluentDocker.Services;
using Microsoft.Extensions.Logging;
using Silverback.TestBench.Containers.Models;
using Silverback.TestBench.Producer;
using Silverback.TestBench.Utils;

namespace Silverback.TestBench.Containers;

public sealed partial class ContainerLogParser : IDisposable
{
    private readonly IContainerService _containerService;

    private readonly MessagesTracker _messagesTracker;

    private readonly ILogger _logger;

    private readonly CancellationTokenSource _stoppingTokenSource = new();

    [SuppressMessage("Usage", "VSTHRD110:Observe result of async calls", Justification = "Fire and forget")]
    public ContainerLogParser(IContainerService containerService, MessagesTracker messagesTracker, ILogger logger)
    {
        _containerService = containerService;
        _messagesTracker = messagesTracker;
        _logger = logger;

        Statistics = new ContainerStats(containerService);

        Task.Run(() => TailLogAsync(_stoppingTokenSource.Token));
    }

    public ContainerStats Statistics { get; }

    public void Dispose()
    {
        _stoppingTokenSource.Cancel();
        _stoppingTokenSource.Dispose();
    }

    [GeneratedRegex(@"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} \+\d{2}:\d{2}) \[(?<logLevel>[A-Z]+)\] (?<message>.+)$")]
    private static partial Regex LogLineRegex();

    [GeneratedRegex(@"Application started\.")]
    private static partial Regex StartedRegex();

    [GeneratedRegex(@"Application is shutting down\.\.\.")]
    private static partial Regex StoppedRegex();

    [GeneratedRegex(@"Processing inbound message.*endpointName: (?<topicName>.*?)(?=,.*?messageId: (?<messageId>.+?)(?=,|$))")]
    private static partial Regex MessageProcessingRegex();

    [GeneratedRegex(@"Successfully processed message '(?<messageId>[^']+)' from topic '(?<topicName>[^']+)'")]
    private static partial Regex MessageProcessedRegex();

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

        switch (logLevel)
        {
            case "WRN":
                Statistics.WarningsCount++;
                break;
            case "ERR":
                Statistics.ErrorsCount++;
                break;
            case "FAT":
                Statistics.FatalErrorsCount++;
                break;
            case "INF":
                DateTime timestamp = DateTime.ParseExact(match.Groups["timestamp"].Value, "yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture);
                ParseInfoMessage(match.Groups["message"].Value, timestamp);

                break;
        }
    }

    private void ParseInfoMessage(string message, DateTime timestamp)
    {
        if (MatchProcessing(message))
            return;

        if (MatchProcessed(message))
            return;

        if (MatchStarted(message, timestamp))
            return;

        MatchStopped(message, timestamp);
    }

    private bool MatchProcessing(string message)
    {
        Match match = MessageProcessingRegex().Match(message);

        if (!match.Success)
            return false;

        _messagesTracker.TrackConsumed(match.Groups["topicName"].Value);
        return true;
    }

    private bool MatchProcessed(string message)
    {
        Match match = MessageProcessedRegex().Match(message);
        if (!match.Success)
            return false;

        Statistics.ProcessedMessagesCount++;
        _messagesTracker.TrackProcessed(
            match.Groups["topicName"].Value,
            match.Groups["messageId"].Value);
        return true;
    }

    private bool MatchStarted(string message, DateTime timestamp)
    {
        if (Statistics.Started.HasValue)
            return false;

        Match match = StartedRegex().Match(message);

        if (!match.Success)
            return false;

        Statistics.Started = timestamp;
        return true;
    }

    private void MatchStopped(string message, DateTime timestamp)
    {
        if (Statistics.Stopped.HasValue)
            return;

        Match match = StoppedRegex().Match(message);

        if (!match.Success)
            return;

        Statistics.Stopped = timestamp;
    }
}
