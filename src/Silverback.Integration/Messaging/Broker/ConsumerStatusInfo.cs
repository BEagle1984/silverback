// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Broker;

internal sealed class ConsumerStatusInfo : IConsumerStatusInfo
{
    private const int MaxHistorySize = 10;

    private readonly LinkedList<IConsumerStatusChange> _history = [];

    private readonly object _syncLock = new();

    public IReadOnlyCollection<IConsumerStatusChange> History => _history;

    public ConsumerStatus Status { get; private set; }

    public int ConsumedMessagesCount { get; private set; }

    public DateTime? LatestConsumedMessageTimestamp { get; private set; }

    public IBrokerMessageIdentifier? LatestConsumedMessageIdentifier { get; private set; }

    public void SetStopped() => ChangeStatus(ConsumerStatus.Stopped);

    public void SetStarted(bool allowStepBack = false)
    {
        lock (_syncLock)
        {
            if (allowStepBack || Status < ConsumerStatus.Started)
                ChangeStatus(ConsumerStatus.Started);
        }
    }

    public void SetConnected()
    {
        lock (_syncLock)
        {
            ChangeStatus(ConsumerStatus.Connected);
        }
    }

    public void RecordConsumedMessage(IBrokerMessageIdentifier? brokerMessageIdentifier)
    {
        lock (_syncLock)
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
