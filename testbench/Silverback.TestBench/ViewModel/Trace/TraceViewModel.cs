// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Silverback.TestBench.ViewModel.Framework;
using Silverback.TestBench.ViewModel.Logs;
using Silverback.TestBench.ViewModel.Topics;

namespace Silverback.TestBench.ViewModel.Trace;

public class TraceViewModel : ViewModelBase
{
    private readonly ConcurrentDictionary<string, MessageTraceViewModel> _traces = new();

    private MessageTraceStatusFilter _statusFilter = MessageTraceStatusFilter.Pending;

    private string? _topicFilter;

    private string? _messageIdFilter;

    private MessageTraceViewModel? _selectedTrace;

    public static IEnumerable<MessageTraceStatusFilter> StatusFilterValues { get; } = Enum.GetValues<MessageTraceStatusFilter>();

    public static IEnumerable<string> TopicFilterValues { get; } = new[] { string.Empty }.Union(TopicNames.All).ToArray();

    public IEnumerable<MessageTraceViewModel> AllTraces => _traces.Values;

    public IEnumerable<MessageTraceViewModel> Traces =>
        _traces.Values
            .Where(
                trace => ApplyStatusFilter(trace.Status) &&
                         (string.IsNullOrEmpty(TopicFilter) || trace.Topic.TopicName == TopicFilter) &&
                         (string.IsNullOrEmpty(MessageIdFilter) || trace.MessageId.Contains(MessageIdFilter, StringComparison.Ordinal)))
            .OrderByDescending(trace => trace.Entries.Last().Timestamp)
            .Take(100)
            .Reverse();

    public MessageTraceViewModel? SelectedTrace
    {
        get => _selectedTrace;
        set => SetProperty(ref _selectedTrace, value, nameof(SelectedTrace));
    }

    public MessageTraceStatusFilter StatusFilter
    {
        get => _statusFilter;
        set
        {
            SetProperty(ref _statusFilter, value, nameof(StatusFilter));
            NotifyPropertyChanged(nameof(Traces));
        }
    }

    public string? TopicFilter
    {
        get => _topicFilter;
        set
        {
            SetProperty(ref _topicFilter, value, nameof(TopicFilter));
            NotifyPropertyChanged(nameof(Traces));
        }
    }

    public string? MessageIdFilter
    {
        get => _messageIdFilter;
        set
        {
            SetProperty(ref _messageIdFilter, value, nameof(MessageIdFilter));
            NotifyPropertyChanged(nameof(Traces));
        }
    }

    public void TraceProduced(string messageId, TopicViewModel topic, LogEntry logEntry) =>
        Trace(messageId, topic, MessageTraceStatus.Produced, logEntry);

    public void TraceProcessing(string messageId, TopicViewModel topic, LogEntry logEntry) =>
        Trace(messageId, topic, MessageTraceStatus.Processing, logEntry);

    // TODO: Trace processing error
    public void TraceProcessingError(string messageId, TopicViewModel topic, LogEntry logEntry) =>
        Trace(messageId, topic, MessageTraceStatus.ProcessingError, logEntry);

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

    private void Trace(string messageId, TopicViewModel topic, MessageTraceStatus status, LogEntry logEntry)
    {
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
