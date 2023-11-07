// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Broker.Mqtt;

internal class ConsumerChannel : ConsumerChannel<ConsumedApplicationMessage>
{
    public ConsumerChannel(int capacity, string id)
        : base(capacity, id)
    {
    }
}
