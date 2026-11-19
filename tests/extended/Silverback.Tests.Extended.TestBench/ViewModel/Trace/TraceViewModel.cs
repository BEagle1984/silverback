// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Silverback.Tests.Extended.TestBench.Shared;
using Silverback.Tests.Extended.TestBench.ViewModel.Framework;
using Silverback.Tests.Extended.TestBench.ViewModel.Logs;
using Silverback.Tests.Extended.TestBench.ViewModel.Topics;

namespace Silverback.Tests.Extended.TestBench.ViewModel.Trace;

public class TraceViewModel : ViewModelBase
{
    private readonly ConcurrentDictionary<string, MessageTraceViewModel> _traces = new();

    public static IEnumerable<MessageTraceStatusFilter> StatusFilterValues { get; } = Enum.GetValues<MessageTraceStatusFilter>();

    public static IEnumerable<string> TopicFilterValues { get; } = [.. new[] { "" }.Union(TopicNames.All)];

    public IEnumerable<MessageTraceViewModel> AllTraces => _traces.Values;

    public IEnumerable<MessageTraceViewModel> Traces =>
        _traces.Values
            .Where(
                trace => ApplyStatusFilter(trace.Status) &&
                         (string.IsNullOrEmpty(TopicFilter) || trace.Topic.TopicName == TopicFilter) &&
                         (string.IsNullOrEmpty(MessageIdFilter) || trace.MessageId.Contains(MessageIdFilter, StringComparison.Ordinal)))
            .OrderByDescending(trace => trace.Entries.Last().Timestamp)
            .Take(200)
            .Reverse();

    public MessageTraceViewModel? SelectedTrace
    {
        get;
        set => SetProperty(ref field, value, nameof(SelectedTrace));
    }

    public MessageTraceStatusFilter StatusFilter
    {
        get;
        set
        {
            SetProperty(ref field, value, nameof(StatusFilter));
            NotifyPropertyChanged(nameof(Traces));
        }
    } = MessageTraceStatusFilter.Pending;

    public string? TopicFilter
    {
        get;
        set
        {
            SetProperty(ref field, value, nameof(TopicFilter));
            NotifyPropertyChanged(nameof(Traces));
        }
    }

    public string? MessageIdFilter
    {
        get;
        set
        {
            SetProperty(ref field, value, nameof(MessageIdFilter));
            NotifyPropertyChanged(nameof(Traces));
        }
    }

    public void TraceProduced(string messageId, TopicViewModel topic, LogEntry logEntry) =>
        Trace(messageId, topic, MessageTraceStatus.Produced, logEntry);

    // Only the messages already traced are updated, to avoid leaving the leftovers of previous sessions pending forever
    public void TraceProcessing(string messageId, TopicViewModel topic, LogEntry logEntry) =>
        Trace(messageId, topic, MessageTraceStatus.Processing, logEntry, false);

    public void TraceProcessingError(string messageId, TopicViewModel topic, LogEntry logEntry) =>
        Trace(messageId, topic, MessageTraceStatus.ProcessingError, logEntry, false);

    public void TraceLost(string messageId, TopicViewModel topic, LogEntry logEntry) =>
        Trace(messageId, topic, MessageTraceStatus.Lost, logEntry);

    public void TraceProcessed(string messageId, TopicViewModel topic, LogEntry logEntry) =>
        Trace(messageId, topic, MessageTraceStatus.Processed, logEntry);

    private bool ApplyStatusFilter(MessageTraceStatus status) =>
        StatusFilter switch
        {
            MessageTraceStatusFilter.Pending => status is not MessageTraceStatus.Processed and not MessageTraceStatus.Lost,
            MessageTraceStatusFilter.NotConsumed => status is MessageTraceStatus.Produced,
            MessageTraceStatusFilter.Error => status is MessageTraceStatus.ProcessingError or MessageTraceStatus.Lost,
            MessageTraceStatusFilter.Processed => status is MessageTraceStatus.Processed,
            _ => true
        };

    private void Trace(string messageId, TopicViewModel topic, MessageTraceStatus status, LogEntry logEntry, bool createIfMissing = true)
    {
        if (!createIfMissing && !_traces.ContainsKey(messageId))
            return;

        _traces.AddOrUpdate(
            messageId,
            _ =>
            {
                MessageTraceViewModel trace = new(messageId, topic, status);
                trace.AddEntry(new MessageTraceEntry(logEntry.Timestamp, status, logEntry));
                return trace;
            },
            (_, trace) =>
            {
                trace.Status = status;
                trace.AddEntry(new MessageTraceEntry(logEntry.Timestamp, status, logEntry));
                return trace;
            });

        NotifyPropertyChanged(nameof(Traces));
        NotifyPropertyChanged(nameof(AllTraces));
    }
}
