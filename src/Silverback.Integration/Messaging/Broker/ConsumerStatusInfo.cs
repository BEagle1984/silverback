// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Broker;

internal sealed class ConsumerStatusInfo : IConsumerStatusInfo
{
    private const int MaxHistorySize = 10;

    private readonly LinkedList<IConsumerStatusChange> _history = [];

    public IReadOnlyCollection<IConsumerStatusChange> History => _history;

    public ConsumerStatus Status { get; private set; }

    public int ConsumedMessagesCount { get; private set; }

    public DateTime? LatestConsumedMessageTimestamp { get; private set; }

    public IBrokerMessageIdentifier? LatestConsumedMessageIdentifier { get; private set; }

    public void SetStopped() => ChangeStatus(ConsumerStatus.Stopped);

    public void SetStarted(bool allowStepBack = false)
    {
        if (allowStepBack || Status < ConsumerStatus.Started)
            ChangeStatus(ConsumerStatus.Started);
    }

    public void SetConnected() => ChangeStatus(ConsumerStatus.Connected);

    public void RecordConsumedMessage(IBrokerMessageIdentifier? brokerMessageIdentifier)
    {
        lock (_history)
        {
            if (Status is ConsumerStatus.Started or ConsumerStatus.Connected)
                ChangeStatus(ConsumerStatus.Consuming);

            ConsumedMessagesCount++;
            LatestConsumedMessageTimestamp = DateTime.Now;
            LatestConsumedMessageIdentifier = brokerMessageIdentifier;
        }
    }

    private void ChangeStatus(ConsumerStatus status)
    {
        Status = status;

        if (_history.Count == MaxHistorySize)
            _history.RemoveFirst();

        _history.AddLast(new ConsumerStatusChange(status));
    }
}
