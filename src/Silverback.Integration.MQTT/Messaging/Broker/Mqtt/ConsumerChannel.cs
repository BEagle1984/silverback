// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Globalization;
using Silverback.Diagnostics;

namespace Silverback.Messaging.Broker.Mqtt;

internal class ConsumerChannel : ConsumerChannel<ConsumedApplicationMessage>
{
    public ConsumerChannel(int capacity, int index, ISilverbackLogger logger)
        : base(capacity, index.ToString(CultureInfo.InvariantCulture), logger)
    {
    }
}
