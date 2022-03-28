// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Broker;

internal sealed class ConsumerStatusInfo : IConsumerStatusInfo
{
    private readonly List<IConsumerStatusChange> _history = new();

    public IReadOnlyCollection<IConsumerStatusChange> History => _history;

    public ConsumerStatus Status { get; private set; }

    public int ConsumedMessagesCount { get; private set; }

    public DateTime? LatestConsumedMessageTimestamp { get; private set; }

    public IBrokerMessageIdentifier? LatestConsumedMessageIdentifier { get; private set; }

    public void SetDisconnected() => ChangeStatus(ConsumerStatus.Stopped);

    public void SetConnected(bool allowStepBack = false)
    {
        if (allowStepBack || Status < ConsumerStatus.Started)
            ChangeStatus(ConsumerStatus.Started);
    }

    public void SetReady() => ChangeStatus(ConsumerStatus.Connected);

    public void RecordConsumedMessage(IBrokerMessageIdentifier? brokerMessageIdentifier)
    {
        if (Status is ConsumerStatus.Started or ConsumerStatus.Connected)
            ChangeStatus(ConsumerStatus.Consuming);

        ConsumedMessagesCount++;
        LatestConsumedMessageTimestamp = DateTime.Now;
        LatestConsumedMessageIdentifier = brokerMessageIdentifier;
    }

    private void ChangeStatus(ConsumerStatus status)
    {
        Status = status;
        _history.Add(new ConsumerStatusChange(status));
    }
}
