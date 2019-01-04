// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Messaging.Broker
{
    public interface IConsumer
    {
        event ReceivedEventHandler Received;

        void Acknowledge(object offset);

        void Acknowledge(IEnumerable<object> offsets);
    }
}