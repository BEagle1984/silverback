// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Broker
{
    internal sealed class ConsumerStatusInfo : IConsumerStatusInfo
    {
        private const int MaxHistorySize = 10;

        private readonly LinkedList<IConsumerStatusChange> _history = new();

        public IReadOnlyCollection<IConsumerStatusChange> History => _history;

        public ConsumerStatus Status { get; private set; }

        public int ConsumedMessagesCount { get; private set; }

        public DateTime? LatestConsumedMessageTimestamp { get; private set; }

        public IBrokerMessageIdentifier? LatestConsumedMessageIdentifier { get; private set; }

        public void SetDisconnected() => ChangeStatus(ConsumerStatus.Disconnected);

        public void SetConnected(bool allowStepBack = false)
        {
            if (allowStepBack || Status < ConsumerStatus.Connected)
                ChangeStatus(ConsumerStatus.Connected);
        }

        public void SetReady() => ChangeStatus(ConsumerStatus.Ready);

        public void RecordConsumedMessage(IBrokerMessageIdentifier? brokerMessageIdentifier)
        {
            if (Status is ConsumerStatus.Connected or ConsumerStatus.Ready)
                ChangeStatus(ConsumerStatus.Consuming);

            ConsumedMessagesCount++;
            LatestConsumedMessageTimestamp = DateTime.Now;
            LatestConsumedMessageIdentifier = brokerMessageIdentifier;
        }

        private void ChangeStatus(ConsumerStatus status)
        {
            Status = status;

            if (_history.Count == MaxHistorySize)
                _history.RemoveFirst();

            _history.AddLast(new ConsumerStatusChange(status));
        }
    }
}
