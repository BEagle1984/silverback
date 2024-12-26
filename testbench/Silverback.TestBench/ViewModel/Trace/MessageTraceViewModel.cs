// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.ObjectModel;
using Silverback.TestBench.ViewModel.Framework;
using Silverback.TestBench.ViewModel.Topics;

namespace Silverback.TestBench.ViewModel.Trace;

public class MessageTraceViewModel : ViewModelBase
{
    private MessageTraceStatus _status;

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

    public ObservableCollection<MessageTraceEntry> Entries { get; } = [];
}
