// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Silverback.TestBench.ViewModel.Framework;
using Silverback.TestBench.ViewModel.Topics;

namespace Silverback.TestBench.ViewModel.Trace;

public class MessageTraceViewModel : ViewModelBase
{
    private readonly ConcurrentBag<MessageTraceEntry> _entries = [];

    private MessageTraceStatus _status;

    private MessageTraceEntry? _selectedEntry;

    public MessageTraceViewModel(string messageId, TopicViewModel topic, MessageTraceStatus status)
    {
        MessageId = messageId;
        Topic = topic;
        _status = status;
    }

    public string MessageId { get; }

    public TopicViewModel Topic { get; }

    public MessageTraceStatus Status
    {
        get => _status;
        set
        {
            if (value > _status)
                SetProperty(ref _status, value, nameof(Status));
        }
    }

    public IEnumerable<MessageTraceEntry> Entries =>
        _entries.OrderBy(entry => entry.Timestamp).ThenBy(entry => entry.LogEntry?.Container?.ContainerService.Name);

    public MessageTraceEntry? SelectedEntry
    {
        get => _selectedEntry;
        set => SetProperty(ref _selectedEntry, value, nameof(SelectedEntry));
    }

    public void AddEntry(MessageTraceEntry entry)
    {
        _entries.Add(entry);
        NotifyPropertyChanged(nameof(Entries));
    }
}
