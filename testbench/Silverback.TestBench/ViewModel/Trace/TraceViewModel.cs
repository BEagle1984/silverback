// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Silverback.TestBench.ViewModel.Framework;
using Silverback.TestBench.ViewModel.Topics;

namespace Silverback.TestBench.ViewModel.Trace;

public class TraceViewModel : ViewModelBase
{
    private readonly ConcurrentDictionary<string, MessageTraceViewModel> _traces = new();

    public IEnumerable<MessageTraceViewModel> Traces => _traces.Values;

    public IEnumerable<MessageTraceViewModel> PendingTraces => _traces.Values.Where(trace => trace.Status != MessageTraceStatus.Processed);

    public void TraceProduced(string messageId, TopicViewModel topic) => _traces.AddOrUpdate(
        messageId,
        _ => new MessageTraceViewModel(messageId, topic, MessageTraceStatus.Produced),
        (_, trace) =>
        {
            trace.Status = MessageTraceStatus.Produced;
            trace.Entries.Add(new MessageTraceEntry(DateTime.Now, MessageTraceStatus.Produced, null));
            return trace;
        });
}
