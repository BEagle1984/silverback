// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Broker
{
    public interface IConsumer
    {
        event EventHandler<MessageReceivedEventArgs> Received;

        void Acknowledge(IOffset offset);

        void Acknowledge(IEnumerable<IOffset> offsets);
    }
}