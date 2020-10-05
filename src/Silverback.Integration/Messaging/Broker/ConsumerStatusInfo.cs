// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Broker
{
    internal class ConsumerStatusInfo : IConsumerStatusInfo
    {
        private readonly List<IConsumerStatusChange> _history = new List<IConsumerStatusChange>();

        public ConsumerStatus Status { get; private set; }

        public IReadOnlyCollection<IConsumerStatusChange> History => _history;

        public int ConsumedMessagesCount { get; private set; }

        public DateTime? LatestConsumedMessageTimestamp { get; private set; }

        public IOffset? LatestConsumedMessageOffset { get; private set; }

        public void SetConnected() => ChangeStatus(ConsumerStatus.Connected);

        public void SetDisconnected() => ChangeStatus(ConsumerStatus.Disconnected);

        public void RecordConsumedMessage(IOffset? offset)
        {
            if (Status == ConsumerStatus.Connected)
                ChangeStatus(ConsumerStatus.Consuming);

            ConsumedMessagesCount++;
            LatestConsumedMessageTimestamp = DateTime.Now;
            LatestConsumedMessageOffset = offset;
        }

        private void ChangeStatus(ConsumerStatus status)
        {
            Status = status;
            _history.Add(new ConsumerStatusChange(status));
        }
    }
}
