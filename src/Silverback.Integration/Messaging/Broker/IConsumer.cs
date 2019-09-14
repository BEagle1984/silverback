// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Messaging.Broker
{
    public interface IConsumer
    {
        event MessageReceivedHandler Received;

        Task Acknowledge(IOffset offset);

        Task Acknowledge(IEnumerable<IOffset> offsets);
    }
}